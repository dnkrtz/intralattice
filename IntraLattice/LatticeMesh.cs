﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino;

// Based on Exoskeleton by David Stasiuk.
// This component generates a solid mesh in place of the lattice frame.
// It takes as input a list of lines and two radius lists (start-end).
// Assumption: none

namespace IntraLattice
{
    public class LatticeMesh : GH_Component
    {

        public LatticeMesh()
            : base("LatticeMesh", "LatticeMesh",
                "Generates solid mesh of lattice wireframe.",
                "IntraLattice2", "Mesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "L", "Line network", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius (start)", "Rs", "List of radii for start of struts", GH_ParamAccess.list, 0.6);
            pManager.AddNumberParameter("Radius (end)", "Re", "List of radii for end of struts", GH_ParamAccess.list, 0.6);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Vertices", "V", "Lattice Mesh Vertices", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Lattice Mesh", GH_ParamAccess.item);
            pManager.AddCurveParameter("Lines", "L", "Lattice Wireframe", GH_ParamAccess.list);
            pManager.AddMeshParameter("Nodes", "P", "Lattice Nodes", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables
            List<Line> L = new List<Line>();
            List<double> Rs = new List<double>();
            List<double> Re = new List<double>();

            // Attempt to fetch data inputs
            if (!DA.GetDataList(0, L)) { return; }
            if (!DA.GetDataList(1, Rs)) { return; }
            if (!DA.GetDataList(2, Re)) { return; }

            // Validate data
            if (L == null || L.Count == 0) { return; }
            if (Rs == null || Rs.Count == 0 || Rs.Contains(0)) { return; }
            if (Re == null || Re.Count == 0 || Rs.Contains(0)) { return; }

            // Number of sides on each strut
            int S = 6;

            //====================================================================================
            // STEP 1 - Data structure
            // In this section, the network of lines and nodes is structured.
            // See MeshTools.cs for descriptions of the two objects (LatticePlate and LatticeNode)
            //====================================================================================

            // Initialize lists of objects
            List<LatticePlate> Plates = new List<LatticePlate>();
            List<LatticeNode> Nodes = new List<LatticeNode>();
            // To avoid creating duplicates nodes, this list stores which nodes have been created
            Point3dList NodeLookup = new Point3dList();

            // Cycle through all the struts, building the model as we go
            for (int i = 0; i < L.Count; i++)
            {
                // Define plates for current strut
                Plates.Add(new LatticePlate());     // PlatePoints[2*i+0] (from)
                Plates.Add(new LatticePlate());     // PlatePoints[2*i+1] (to)
                Plates[2 * i].Radius = Rs[i % Rs.Count];
                Plates[2 * i + 1].Radius = Re[i % Re.Count];
                Plates[2 * i].Normal = L[i].UnitTangent;
                Plates[2 * i + 1].Normal = -Plates[2 * i].Normal;

                // Setup nodes by checking endpoints of strut
                List<Point3d> Pts = new List<Point3d>();
                Pts.Add(L[i].From); Pts.Add(L[i].To);   // Start point first

                // Loops over the 2 nodes, updating the lattice model
                for (int j = 0; j < 2; j++)
                {
                    int NodeIndex;

                    int NI = NodeLookup.ClosestIndex(Pts[j]);

                    // Check if node already exists (also, catch first iteration)
                    if (i != 0 && NodeLookup[NI].DistanceTo(Pts[j]) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                        NodeIndex = NI;
                    }
                    // If node doesn't exist, create it and update the nodelookup list
                    else
                    {
                        Nodes.Add(new LatticeNode(Pts[j]));
                        NodeIndex = Nodes.Count - 1;
                        NodeLookup.Add(Pts[j]);
                    }

                    Plates[2 * i + j].NodeIndex = NodeIndex;        // 2*i+j is the correct index, recall that we must order them start to finish
                    Nodes[NodeIndex].PlateIndices.Add(2 * i + j);

                }
            }

            //====================================================================================
            // STEP 2 - Compute plate offsets
            // In this section, the plate offsets are computed to avoid overlapping sleeve meshes
            //====================================================================================

            // Loop over all nodes
            for (int i = 0; i < Nodes.Count; i++)
            {
                double TestOffset = 0;

                // Loop over all possible pairs of plates on the node
                // This automatically avoids setting offsets for nodes with a single strut
                for (int j = 0; j < Nodes[i].PlateIndices.Count; j++)
                {
                    for (int k = j + 1; k < Nodes[i].PlateIndices.Count; k++)
                    {
                        int I1 = Nodes[i].PlateIndices[j];
                        int I2 = Nodes[i].PlateIndices[k];

                        // Evaluate based on largest radius
                        double R1 = Plates[I1].Radius;
                        double R2 = Plates[I2].Radius;
                        double R = Math.Max(R1, R2);
                        // Compute angle between normals
                        double Theta = Vector3d.VectorAngle(Plates[I1].Normal, Plates[I2].Normal);

                        // If theta is more than 90deg, offset is simply based on a sphere at the node
                        if (Theta >= Math.PI * 0.5) TestOffset = R / Math.Cos(Math.PI / S);
                        // Else, use simple trig
                        else TestOffset = R / Math.Sin(Theta * 0.5);

                        // If current test offset is greater (could be faster if we just set these in the loop below)
                        // But it wouldn't support variable offsets, which are beneficial in some scenarios
                        if (TestOffset > Plates[I1].Offset) Plates[I1].Offset = TestOffset;
                        if (TestOffset > Plates[I2].Offset) Plates[I2].Offset = TestOffset;

                    }
                }

                // Set the plate locations
                foreach (int P in Nodes[i].PlateIndices)
                {
                    LatticePlate Plate = Plates[P];
                    Plates[P].Vtc.Add(Nodes[Plate.NodeIndex].Point3d + Plate.Normal * Plate.Offset);    // add plate centerpoint
                }

            }

            //====================================================================================
            // STEP 3 - Build actual mesh
            // In this section, we compute all the sleeve (strut) and hull (node) meshes
            // Recall, coincident points between the strut & hull meshes are the plate vertices
            //====================================================================================

            // Initialize the output mesh
            Mesh FullMesh = new Mesh();

            // SLEEVES - Loop over all pairs of plates (struts)
            // Create all plate vertices and sleeve vertices
            for (int i = 0; i < L.Count; i++)
            {
                Mesh SleeveMesh = new Mesh();
                double AvgRadius = (Plates[2 * i].Radius + Plates[2 * i + 1].Radius) / 2;
                double Length = Plates[2 * i].Vtc[0].DistanceTo(Plates[2 * i + 1].Vtc[0]);
                double D = Math.Max((Math.Round(Length * 0.5 / AvgRadius) * 2), 2); // Number of sleeve divisions (must be even)

                // Create sleeve vertices
                // Loops: j along strut, k around strut
                for (int j = 0; j <= D; j++)
                {
                    Point3d Knuckle = Plates[2 * i].Vtc[0] + (Plates[2 * i].Normal * (Length * j / D));
                    Plane plane = new Plane(Knuckle, Plates[2 * i].Normal);
                    double R = Plates[2 * i].Radius - j / (double)D * (Plates[2 * i].Radius - Plates[2 * i + 1].Radius); //variable radius

                    for (int k = 0; k < S; k++)
                    {
                        double angle = k * 2 * Math.PI / S + j * Math.PI / S;
                        SleeveMesh.Vertices.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle))); // create vertex

                        // if hullpoints, save them for hulling
                        if (j == 0) Plates[2 * i].Vtc.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle)));
                        if (j == D) Plates[2 * i + 1].Vtc.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle)));
                    }
                }

                // Create sleeve faces
                MeshTools.SleeveStitch(ref SleeveMesh, D, S);
                FullMesh.Append(SleeveMesh);

            }


            List<Mesh> HullMeshes = new List<Mesh>();

            // HULLS - Loop over all nodes
            for (int i = 0; i < Nodes.Count; i++)
            {
                int PlateCount = Nodes[i].PlateIndices.Count;
                // If node has a single plate, create an endmesh
                if (PlateCount < 2)
                {
                    Mesh EndMesh = new Mesh();
                    // Add all plate points to mesh vertices
                    foreach (Point3d PlatePoint in Plates[Nodes[i].PlateIndices[0]].Vtc) EndMesh.Vertices.Add(PlatePoint);
                    MeshTools.EndFaceStitch(ref EndMesh, S);
                    FullMesh.Append(EndMesh);
                }
                // If node has more than 1 plate, create a hullmesh
                else
                {
                    Mesh HullMesh = new Mesh();

                    // Gather all hull points (i.e. all plate points of the node)
                    List<Point3d> HullPoints = new List<Point3d>();
                    foreach (int P in Nodes[i].PlateIndices) HullPoints.AddRange(Plates[P].Vtc);
                    MeshTools.ConvexHull(ref HullMesh, HullPoints, S);
                    HullMeshes.Add(HullMesh);
                }
            }

            // POST-PROCESS FINAL MESH
            FullMesh.Vertices.CombineIdentical(true, true);
            FullMesh.FaceNormals.ComputeFaceNormals();
            FullMesh.UnifyNormals();
            FullMesh.Normals.ComputeNormals();

            DA.SetDataList(0, Plates[0].Vtc);
            DA.SetData(1, FullMesh);
            DA.SetDataList(2, L);
            DA.SetDataList(3, HullMeshes);
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{dee24b08-fcb2-46f9-b772-9bece0903d9a}"); }
        }
    }
}