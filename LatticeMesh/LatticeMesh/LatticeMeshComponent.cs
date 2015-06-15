using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;

// Based on Exoskeleton by David Stasiuk.
// This component generates a solid mesh in place of the lattice frame.
// It takes as input a list of lines and two radius lists (start-end).
// Assumption: none

namespace LatticeMesh
{
    public class LatticeMeshComponent : GH_Component
    {

        public LatticeMeshComponent()
            : base("LatticeMesh", "LatticeMesh",
                "Generates solid mesh of lattice wireframe.",
                "IntraLattice2", "Mesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "L", "Line network", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius (start)", "Rs", "List of radii for start of struts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius (end)", "Re", "List of radii for end of struts", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Vertices", "V", "Lattice Mesh Vertices", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Lattice Mesh", GH_ParamAccess.item);
            pManager.AddCurveParameter("Lines", "L", "Lattice Wireframe", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Nodes", "P", "Lattice Nodes", GH_ParamAccess.list);
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

            // STEP 1 - Build Lattice Model
            List<LatticePlate> Plates = new List<LatticePlate>();
            List<LatticeNode> Nodes = new List<LatticeNode>();

            int S = 6;  // number of sides of strut
            Point3dList NodeLookup = new Point3dList(); // This is used to quickly locate node pts

            // Cycle through all the struts, building the model as we go
            for (int i = 0; i < L.Count; i++ )
            {
                // Define plates for current strut
                Plates.Add(new LatticePlate());     // PlatePoints[2*i+0] (from)
                Plates.Add(new LatticePlate());     // PlatePoints[2*i+1] (to)
                Plates[2*i].Radius = Rs[i % Rs.Count];
                Plates[2*i+1].Radius = Re[i % Re.Count];
                Plates[2*i].Normal = L[i].UnitTangent;
                Plates[2*i+1].Normal = - Plates[2*i].Normal;
                
                // Setup nodes (start point first)
                List<Point3d> Pts = new List<Point3d>();
                Pts.Add(L[i].From); Pts.Add(L[i].To);

                // Loops over the 2 nodes, updating the lattice model
                for (int j=0; j<2; j++)
                {
                    int NodeIndex;
                    
                    // check if node already exists (FIX THIS - NEED TO USE A TOLERANCE PARAMETER)
                    if (NodeLookup.Contains(Pts[j])) NodeIndex = NodeLookup.ClosestIndex(Pts[j]);
                    // if it doesn't exist, create it and update the nodelookup list
                    else
                    {
                        Nodes.Add(new LatticeNode(Pts[j]));
                        NodeIndex = Nodes.Count - 1;
                        NodeLookup.Add(Pts[j]);             // node won't be created again
                    }

                    Plates[2*i+j].NodeIndex = NodeIndex;        // 2*i+j is the correct index, recall that we must order them start to finish
                    Nodes[NodeIndex].PlateIndices.Add(2*i+j);
        
                }
            }

            // STEP 2 - Compute Plate Offsets
            for (int i=0; i<Nodes.Count; i++)   // Loop over all nodes
            {
                double Offset = 0;

                // Loop over all possible pairs of plates on the node
                for (int j = 0; j < Nodes[i].PlateIndices.Count; j++) 
                {
                    for (int k = j + 1; k < Nodes[i].PlateIndices.Count; k++)
                    {

                        // Minimum offset is the strut radius
                        Offset = Plates[Nodes[i].PlateIndices[0]].Radius / Math.Cos(Math.PI / S);        // this is sloppy, fix later

                        double Theta = Vector3d.VectorAngle(Plates[j].Normal, Plates[k].Normal);
                        double StepOffset = Offset * Math.Cos(Theta * 0.5) / Math.Sin(Theta * 0.5);

                    }
                }
                // Set the plate locations
                foreach (int index in Nodes[i].PlateIndices)
                {
                    LatticePlate plate = Plates[index];
                    plate.Offset = Offset;
                    plate.Vtc.Add(Nodes[plate.NodeIndex].Point3d + plate.Normal * plate.Offset);    // add plate centerpoints
                }

            }

            
            // STEP 3 - Build Mesh
            Mesh FullMesh = new Mesh(); // This is what will be output, contains all meshes
            Mesh strutmesh;
            
            // Create sleeve vertices
            // Here, as we create the vertices of the sleeve mesh, we also save the PlatePoints
            for (int i=0; i < L.Count; i++)
            {
                strutmesh = new Mesh();
                double avgRadius = (Plates[2*i].Radius + Plates[2*i+1].Radius) / 2;
                double length = Plates[2*i].Vtc[0].DistanceTo(Plates[2*i+1].Vtc[0]);
                double D = (Math.Round(length * 0.5 / avgRadius) * 2) + 2; // Number of sleeve divisions (must be even)

                // Create sleeve vertices
                // j-loop travels along strut
                for (int j = 0; j <= D; j++)
                {
                    Point3d Knuckle = Plates[2*i].Vtc[0] + (Plates[2*i].Normal * ( length * j / D));
                    Plane plane = new Plane(Knuckle, Plates[2*i].Normal);
                    double radius = Plates[2*i].Radius - j / (double)D * (Plates[2*i].Radius - Plates[2*i+1].Radius); //variable radius
                    
                    // k-loop travels about strut
                    for (int k = 0; k < S; k++)
                    {
                        double angle = k * 2 * Math.PI / S + j * Math.PI / S;
                        strutmesh.Vertices.Add(plane.PointAt(radius * Math.Cos(angle), radius * Math.Sin(angle))); // create vertex

                        // if hullpoints, save them for hulling
                        if (j == 0) Plates[2*i].Vtc.Add(plane.PointAt(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                        if (j == D) Plates[2*i+1].Vtc.Add(plane.PointAt(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                    }
                }

                // Create sleeve faces
                MeshTools.SleeveStitch(ref strutmesh, D, S);
                FullMesh.Append(strutmesh);

            }              

            // ENDFACE MESHES (DUMMY HULL)
            Mesh endmesh = new Mesh();
            Mesh endmesh2 = new Mesh();

            foreach (Point3d PlatePoint in Plates[0].Vtc)
            {
                endmesh.Vertices.Add(PlatePoint);
            }
            foreach (Point3d PlatePoint2 in Plates[1].Vtc)
            {
                endmesh2.Vertices.Add(PlatePoint2);
            }            

            MeshTools.EndFaceStitch(ref endmesh, S);
            FullMesh.Append(endmesh);

            MeshTools.EndFaceStitch(ref endmesh2, S);
            FullMesh.Append(endmesh2);

            // POST-PROCESS FINAL MESH
            FullMesh.Vertices.CombineIdentical(true, true);
            FullMesh.FaceNormals.ComputeFaceNormals();
            FullMesh.UnifyNormals();
            FullMesh.Normals.ComputeNormals();

            DA.SetDataList(0, Plates[0].Vtc);
            DA.SetData(1, FullMesh);
            DA.SetDataList(2, L);
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
