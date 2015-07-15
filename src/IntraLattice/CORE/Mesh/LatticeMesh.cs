using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino;
using IntraLattice.Properties;

// This component converts the wireframe lattice into a solid mesh.
// ================================================================
// Based on Exoskeleton by David Stasiuk.
// It takes as input a list of lines and two radius lists (start-end).

// Written by Aidan Kurtz (http://aidankurtz.com)

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
            List<Line> lineList = new List<Line>();
            List<double> startRadiusList = new List<double>();
            List<double> endRadiusList = new List<double>();

            // Attempt to fetch data inputs
            if (!DA.GetDataList(0, lineList)) { return; }
            if (!DA.GetDataList(1, startRadiusList)) { return; }
            if (!DA.GetDataList(2, endRadiusList)) { return; }

            // Validate data
            if (lineList == null || lineList.Count == 0) { return; }
            if (startRadiusList == null || startRadiusList.Count == 0 || startRadiusList.Contains(0)) { return; }
            if (endRadiusList == null || endRadiusList.Count == 0 || startRadiusList.Contains(0)) { return; }

            // Number of sides on each strut
            int sides = 6;

            //====================================================================================
            // STEP 1 - Data structure
            // In this section, the network of lines and nodes is structured.
            // See MeshTools.cs for descriptions of the two objects (LatticePlate and LatticeNode)
            //====================================================================================

            // Initialize lists of objects
            List<LatticePlate> plates = new List<LatticePlate>();
            List<LatticeNode> nodes = new List<LatticeNode>();
            // To avoid creating duplicates nodes, this list stores which nodes have been created
            Point3dList nodeLookup = new Point3dList();

            // Cycle through all the struts, building the model as we go
            for (int i = 0; i < lineList.Count; i++)
            {
                // Define plates for current strut
                plates.Add(new LatticePlate());     // PlatePoints[2*i+0] (from)
                plates.Add(new LatticePlate());     // PlatePoints[2*i+1] (to)
                plates[2 * i].Radius = startRadiusList[i % startRadiusList.Count];
                plates[2 * i + 1].Radius = endRadiusList[i % endRadiusList.Count];
                plates[2 * i].Normal = lineList[i].UnitTangent;
                plates[2 * i + 1].Normal = -plates[2 * i].Normal;

                // Setup nodes by checking endpoints of strut
                List<Point3d> pts = new List<Point3d>();
                pts.Add(lineList[i].From); pts.Add(lineList[i].To);   // Start point first

                // Loops over the 2 nodes, updating the lattice model
                for (int j = 0; j < 2; j++)
                {
                    int nodeIndex;

                    int NI = nodeLookup.ClosestIndex(pts[j]);

                    // Check if node already exists (also, catch first iteration)
                    if (i != 0 && nodeLookup[NI].DistanceTo(pts[j]) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                        nodeIndex = NI;
                    }
                    // If node doesn't exist, create it and update the nodelookup list
                    else
                    {
                        nodes.Add(new LatticeNode(pts[j]));
                        nodeIndex = nodes.Count - 1;
                        nodeLookup.Add(pts[j]);
                    }

                    plates[2 * i + j].NodeIndex = nodeIndex;        // 2*i+j is the correct index, recall that we must order them start to finish
                    nodes[nodeIndex].PlateIndices.Add(2 * i + j);

                }
            }

            //====================================================================================
            // STEP 2 - Compute plate offsets
            // In this section, the plate offsets are computed to avoid overlapping sleeve meshes
            //====================================================================================

            // Loop over all nodes
            for (int i = 0; i < nodes.Count; i++)
            {
                double testOffset = 0;

                // Loop over all possible pairs of plates on the node
                // This automatically avoids setting offsets for nodes with a single strut
                for (int j = 0; j < nodes[i].PlateIndices.Count; j++)
                {
                    for (int k = j + 1; k < nodes[i].PlateIndices.Count; k++)
                    {
                        int I1 = nodes[i].PlateIndices[j];
                        int I2 = nodes[i].PlateIndices[k];

                        // Evaluate based on largest radius
                        double R1 = plates[I1].Radius;
                        double R2 = plates[I2].Radius;
                        double R = Math.Max(R1, R2);
                        // Compute angle between normals
                        double theta = Vector3d.VectorAngle(plates[I1].Normal, plates[I2].Normal);

                        // If theta is more than 90deg, offset is simply based on a sphere at the node
                        if (theta >= Math.PI * 0.5) testOffset = R / Math.Cos(Math.PI / sides);
                        // Else, use simple trig
                        else testOffset = R / Math.Sin(theta * 0.5);

                        // If current test offset is greater (could be faster if we just set these in the loop below)
                        // But it wouldn't support variable offsets, which are beneficial in some scenarios
                        if (testOffset > plates[I1].Offset) plates[I1].Offset = testOffset;
                        if (testOffset > plates[I2].Offset) plates[I2].Offset = testOffset;

                    }
                }

                // Set the plate locations
                foreach (int P in nodes[i].PlateIndices)
                {
                    LatticePlate plate = plates[P];
                    plates[P].Vtc.Add(nodes[plate.NodeIndex].Point3d + plate.Normal * plate.Offset);    // add plate centerpoint
                }

            }

            //====================================================================================
            // STEP 3 - Build actual mesh
            // In this section, we compute all the sleeve (strut) and hull (node) meshes
            // Recall, coincident points between the strut & hull meshes are the plate vertices
            //====================================================================================

            // Initialize the output mesh
            Mesh fullMesh = new Mesh();

            // SLEEVES - Loop over all pairs of plates (struts)
            // Create all plate vertices and sleeve vertices
            for (int i = 0; i < lineList.Count; i++)
            {
                Mesh sleeveMesh = new Mesh();
                double avgRadius = (plates[2 * i].Radius + plates[2 * i + 1].Radius) / 2;
                double length = plates[2 * i].Vtc[0].DistanceTo(plates[2 * i + 1].Vtc[0]);
                double divisions = Math.Max((Math.Round(length * 0.5 / avgRadius) * 2), 2); // Number of sleeve divisions (must be even)

                // Create sleeve vertices
                // Loops: j along strut, k around strut
                for (int j = 0; j <= divisions; j++)
                {
                    Point3d knucklePt = plates[2 * i].Vtc[0] + (plates[2 * i].Normal * (length * j / divisions));
                    Plane plane = new Plane(knucklePt, plates[2 * i].Normal);
                    double R = plates[2 * i].Radius - j / (double)divisions * (plates[2 * i].Radius - plates[2 * i + 1].Radius); //variable radius

                    for (int k = 0; k < sides; k++)
                    {
                        double angle = k * 2 * Math.PI / sides + j * Math.PI / sides;
                        sleeveMesh.Vertices.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle))); // create vertex

                        // if hullpoints, save them for hulling
                        if (j == 0) plates[2 * i].Vtc.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle)));
                        if (j == divisions) plates[2 * i + 1].Vtc.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle)));
                    }
                }

                // Create sleeve faces
                MeshTools.SleeveStitch(ref sleeveMesh, divisions, sides);
                fullMesh.Append(sleeveMesh);

            }

            List<Mesh> hullMeshes = new List<Mesh>();

            // HULLS - Loop over all nodes
            for (int i = 0; i < nodes.Count; i++)
            {
                int plateCount = nodes[i].PlateIndices.Count;
                // If node has a single plate, create an endmesh
                if (plateCount < 2)
                {
                    Mesh endMesh = new Mesh();
                    // Add all plate points to mesh vertices
                    foreach (Point3d platePoint in plates[nodes[i].PlateIndices[0]].Vtc) endMesh.Vertices.Add(platePoint);
                    MeshTools.EndFaceStitch(ref endMesh, sides);
                    fullMesh.Append(endMesh);
                }
                // If node has more than 1 plate, create a hullmesh
                else
                {
                    Mesh hullMesh = new Mesh();

                    // Gather all hull points (i.e. all plate points of the node)
                    List<Point3d> hullPoints = new List<Point3d>();
                    foreach (int pIndex in nodes[i].PlateIndices) hullPoints.AddRange(plates[pIndex].Vtc);
                    MeshTools.ConvexHull(ref hullMesh, hullPoints, sides);
                    
                    Point3d[] hullVtcs = hullMesh.Vertices.ToPoint3dArray();

                    for (int vtcIndex=0; vtcIndex<hullVtcs.Length; vtcIndex++)
                    {
                        Point3d hullVtx = hullVtcs[vtcIndex];
                        int[] meshFaces = hullMesh.Vertices.GetVertexFaces(vtcIndex);

                        for (int faceIndex = 0; faceIndex<meshFaces.Length; faceIndex++)
                        {
                            foreach (int plateIndex in nodes[i].PlateIndices)
                            {

                            }
                        }
                    }

                    hullMeshes.Add(hullMesh);
                }
            }

            // POST-PROCESS FINAL MESH
            fullMesh.Vertices.CombineIdentical(true, true);
            fullMesh.FaceNormals.ComputeFaceNormals();
            fullMesh.UnifyNormals();
            fullMesh.Normals.ComputeNormals();

            DA.SetDataList(0, plates[0].Vtc);
            DA.SetData(1, fullMesh);
            DA.SetDataList(2, lineList);
            DA.SetDataList(3, hullMeshes);
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
                //return Resources.PresetCell;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{dee24b08-fcb2-46f9-b772-9bece0903d9a}"); }
        }
    }
}
