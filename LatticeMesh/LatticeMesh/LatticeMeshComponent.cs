using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;

namespace LatticeMesh
{
    public class LatticeMeshComponent : GH_Component
    {

        public LatticeMeshComponent()
            : base("LatticeMesh", "LatticeMesh",
                "Description",
                "Category", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddLineParameter("Lines", "L", "Line network", GH_ParamAccess.list);
            //pManager.AddPointParameter("Nodes", "P", "Lattice nodes", GH_ParamAccess.tree);
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
            // DUMMY INPUT
            int S = 6;
            List<Point3d> P = new List<Point3d>(); // useless, just for dummy data
            P.Add(new Point3d(0, 0, 0));
            P.Add(new Point3d(0, 0, 50));
            P.Add(new Point3d(0, 50, 0));
            P.Add(new Point3d(50, 0, 0));
            List<LineCurve> L = new List<LineCurve>();
            L.Add(new LineCurve(P[0], P[1]));
            L.Add(new LineCurve(P[0], P[2]));
            L.Add(new LineCurve(P[0], P[3]));
            List<double> Rs = new List<double>();   // start radius
            List<double> Re = new List<double>();   // end radius
            Rs.Add(4); Re.Add(4);
            Rs.Add(4); Re.Add(4);
            Rs.Add(4); Re.Add(4);


            // STEP 1 - BUILD LATTICE MODEL
            List<LatticePlate> Plates = new List<LatticePlate>();
            List<LatticeNode> Nodes = new List<LatticeNode>();

            Point3dList NodeLookup = new Point3dList(); // This is used to quickly locate node pts

            // Cycle through all the struts, building the model as we go
            for (int i = 0; i < L.Count; i++ )
            {
                // Define plates for current strut
                Plates.Add(new LatticePlate());     // PlatePoints[2*i+0] (from)
                Plates.Add(new LatticePlate());     // PlatePoints[2*i+1] (to)
                Plates[2*i].Radius = Rs[i];
                Plates[2*i+1].Radius = Re[i];
                Plates[2*i].Normal = L[i].TangentAtStart;
                Plates[2*i+1].Normal = - Plates[2*i].Normal;
                
                // Setup nodes (start point first)
                List<Point3d> Pts = new List<Point3d>();
                Pts.Add(L[i].Line.From); Pts.Add(L[i].Line.To);

                // Loops over the 2 nodes, updating the lattice model
                for (int j=0; j<2; j++)
                {
                    int NodeIndex;
                    
                    // check if node already exists (TO FIX - NEED TO USE A TOLERANCE PARAMETER)
                    if (NodeLookup.Contains(Pts[j])) NodeIndex = NodeLookup.ClosestIndex(Pts[j]);
                    // if it doesn't exist, create it and update the nodelookup list
                    else
                    {
                        Nodes.Add(new LatticeNode(Pts[j]));
                        NodeIndex = Nodes.Count - 1;
                        NodeLookup.Add(Pts[j]);             // node won't be created again
                    }

                    Plates[2*i+j].NodeIndex = NodeIndex;
                    Nodes[NodeIndex].PlateIndices.Add(2*i+j);
        
                }
            }

            // STEP 2 - COMPUTE PLATE OFFSETS
            
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

            

            // STEP 3 - BUILD MESH
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

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{dee24b08-fcb2-46f9-b772-9bece0903d9a}"); }
        }
    }
}
