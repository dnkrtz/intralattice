using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.Collections;

namespace IntraLattice.CORE.MeshModule
{
    public class Homogen : GH_Component
    {
        public Homogen()
            : base("Homogen","Homogen",
                "Homogeneous solidification of lattice wireframe",
                "IntraLattice2", "Mesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Wireframe to thicken", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius", "Radius", "Strut Radius", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Thickened wireframe", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 0. Declare placeholder variables
            List<Curve> inputStruts = new List<Curve>();
            double radius = 0;

            // 1. Attempt to fetch data inputs
            if (!DA.GetDataList(0, inputStruts)) { return; }
            if (!DA.GetData(1, ref radius)) { return; }

            // 2. Validate data
            if (inputStruts == null || inputStruts.Count == 0) { return; }
            if (radius <= 0) { return; }

            // 3. Set some variables
            int sides = 6;  // Number of sides on each strut
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // 4. Initialize lattice object and output mesh
            LatticeMesh lattice = new LatticeMesh();
            Mesh outMesh = new Mesh();


            //====================================================================================
            //  PART A - Network cleanse
            //  Clean the network of curves
            //  - Remove duplicate nodes and struts
            //  - Remove null, invalid or tiny curves
            //  - (Future idea: Combining colinear struts)
            //====================================================================================

            // A0. We use the following three lists to extract valid data from the input list
            var nodeList = new Point3dList();               // List of unique nodes
            var nodePairList = new List<IndexPair>();   // List of struts, as node index pairs
            var strutList = new List<Curve>();              // List of struts, as curves (parallel to nodePairList)

            MeshTools.CleanNetwork(inputStruts, out nodeList, out nodePairList, out strutList);

            //====================================================================================
            // PART B - Data structure
            // In this section, we construct the wireframe lattice
            // Ensuring that no duplicate nodes or struts are present
            //====================================================================================

            // B0. Create nodes
            foreach (Point3d node in nodeList)
                lattice.Nodes.Add(new Node(node));

            // B1. Create struts and plates
            for (int i = 0; i < strutList.Count; i++)
            {
                lattice.Struts.Add(new Strut(strutList[i], nodePairList[i])); // assign
                // construct plates
                lattice.Plates.Add(new Plate(nodePairList[i].I, strutList[i].TangentAtStart));
                lattice.Plates.Add(new Plate(nodePairList[i].J, -strutList[i].TangentAtEnd));
                // set strut relational parameters
                IndexPair platePair = new IndexPair(lattice.Plates.Count - 2, lattice.Plates.Count - 1);
                lattice.Struts[i].PlatePair = platePair;
                // set node relational parameters
                lattice.Nodes[nodePairList[i].I].StrutIndices.Add(i);
                lattice.Nodes[nodePairList[i].J].StrutIndices.Add(i);
                lattice.Nodes[nodePairList[i].I].PlateIndices.Add(platePair.I);
                lattice.Nodes[nodePairList[i].J].PlateIndices.Add(platePair.J);
            }


            //====================================================================================
            // PART C - Compute nodal radii
            // Strut radius is node-based
            //====================================================================================

            // C0. Set radii
            foreach (Node node in lattice.Nodes)
            {
                node.Radius = radius;
            }


            //====================================================================================
            // PART D - Compute plate offsets
            // Each plate is offset from its parent node, to avoid mesh overlaps.
            //====================================================================================

            // D0. Loop over nodes
            for (int i = 0; i < lattice.Nodes.Count; i++)
            {
                // set the node being evaluated
                Node node = lattice.Nodes[i];
                // if node has only 1 strut, skip it
                if (node.StrutIndices.Count < 2) continue;
                
                // compute the offsets required to avoid plate overlaps
                double offset;
                MeshTools.ComputeOffsets(node, lattice, tol, out offset);

                // To improve the shape of the mesh at 'sharp nodes', we add an extra node plate
                // This plate is in the direction of the negative sum of all normals
                // We only create it if the strut set is 'sharp', meaning the struts are contained in a 180degree peripheral
                bool isSharp = true;
                Vector3d extraNormal = new Vector3d();  // sum of all normals
                foreach (int plateIndex in node.PlateIndices)
                    extraNormal += lattice.Plates[plateIndex].Normal;
                foreach (int plateIndex in node.PlateIndices)
                    if (Vector3d.VectorAngle(-extraNormal, lattice.Plates[plateIndex].Normal) < Math.PI / 2)
                        isSharp = false;

                //  if struts form a sharp corner, add an extra plate for a better convex hull shape
                if (isSharp)
                {
                    List<Point3d> Vtc;
                    // plane offset from node slightly
                    Plane plane = new Plane(node.Point3d - extraNormal / 6, -extraNormal);
                    MeshTools.CreatePlate(plane, sides, node.Radius, 0, out Vtc);    // compute the vertices
                    // add new plate and its vertices
                    lattice.Plates.Add(new Plate(i, -extraNormal));
                    int newPlateIndx = lattice.Plates.Count - 1;
                    lattice.Plates[newPlateIndx].Vtc.AddRange(Vtc);
                    node.PlateIndices.Add(newPlateIndx);
                }

            }

            // IDEA : add a new loop here that adjusts radii to avoid overlapping struts



            //====================================================================================
            // PART E - Construct sleeve meshes and hull points
            // 
            //====================================================================================

            // E0. Loop over struts
            for (int i = 0; i < lattice.Struts.Count; i++)
            {
                Mesh sleeveMesh = new Mesh();

                Strut strut = lattice.Struts[i];
                Plate startPlate = lattice.Plates[strut.PlatePair.I];   // plate for the start of the strut
                Plate endPlate = lattice.Plates[strut.PlatePair.J];
                double startParam, endParam;
                strut.Curve.LengthParameter(startPlate.Offset, out startParam);   // get start and end params of strut (accounting for offset)
                strut.Curve.LengthParameter(strut.Curve.GetLength() - endPlate.Offset, out endParam);
                startPlate.Vtc.Add(strut.Curve.PointAt(startParam));    // set center point of star & end plates
                endPlate.Vtc.Add(strut.Curve.PointAt(endParam));

                // compute the number of divisions
                double length = strut.Curve.GetLength(new Interval(startParam, endParam));
                double divisions = Math.Max((Math.Round(length * 0.5 / radius) * 2), 2); // Number of sleeve divisions (must be even)

                // SLEEVE VERTICES
                // 
                // ================
                // if linear lattice, we don't need to compute the strut tangent more than once
                if (strut.Curve.IsLinear())
                {
                    Vector3d normal = strut.Curve.TangentAtStart;

                    // Loops: j along strut
                    for (int j = 0; j <= divisions; j++)
                    {
                        Point3d knucklePt = startPlate.Vtc[0] + (normal * (length * j / divisions));
                        Plane plane = new Plane(knucklePt, normal);
                        double startAngle = j * Math.PI / sides; // this twists the plate points along the strut, for triangulation

                        List<Point3d> Vtc;
                        MeshTools.CreatePlate(plane, sides, radius, startAngle, out Vtc);    // compute the vertices

                        // if the vertices are hull points (plates that connect sleeves to node hulls), save them
                        if (j == 0) startPlate.Vtc.AddRange(Vtc);
                        if (j == divisions) endPlate.Vtc.AddRange(Vtc);

                        sleeveMesh.Vertices.AddVertices(Vtc); // save vertices to sleeve mes
                    }
                }
                // otherwise, we're dealing with curves, so need to travel along curve and compute tangent frames at each knuckle
                else
                {
                    Vector3d normal = strut.Curve.TangentAtStart;

                    // Loops: j along strut, k around strut
                    for (int j = 0; j <= divisions; j++)
                    {
                        double locParameter = startParam + (j / divisions) * (endParam - startParam);

                        Point3d knucklePt = strut.Curve.PointAt(locParameter);
                        Plane plane;
                        strut.Curve.PerpendicularFrameAt(locParameter, out plane);
                        double startAngle = j * Math.PI / sides; // this twists the plate points along the strut, for triangulation

                        List<Point3d> Vtc;
                        MeshTools.CreatePlate(plane, sides, radius, startAngle, out Vtc);    // compute the vertices

                        // if the vertices are hull points (plates that connect sleeves to node hulls), save them
                        if (j == 0) startPlate.Vtc.AddRange(Vtc);
                        if (j == divisions) endPlate.Vtc.AddRange(Vtc);

                        sleeveMesh.Vertices.AddVertices(Vtc); // save vertices to sleeve mesh

                    }
                }

                // SLEEVE FACES
                MeshTools.SleeveStitch(ref sleeveMesh, divisions, sides);
                outMesh.Append(sleeveMesh);

            }

            //====================================================================================
            // STEP 5 - Construct hull meshes
            // 
            //====================================================================================

            List<Mesh> hullMeshList = new List<Mesh>();

            // HULLS - Loop over all nodes
            for (int i = 0; i < lattice.Nodes.Count; i++)
            {
                Node node = lattice.Nodes[i];

                int plateCount = lattice.Nodes[i].PlateIndices.Count;
                // If node has a single plate, create an endmesh
                if (plateCount < 2)
                {
                    Mesh endMesh = new Mesh();
                    // Add all plate points to mesh vertices
                    foreach (Point3d platePoint in lattice.Plates[node.PlateIndices[0]].Vtc)
                        endMesh.Vertices.Add(platePoint);
                    MeshTools.EndFaceStitch(ref endMesh, sides);
                    outMesh.Append(endMesh);
                }
                // If node has more than 1 plate, create a hullmesh
                else
                {
                    Mesh hullMesh = new Mesh();

                    // Gather all hull points (i.e. all plate points of the node)
                    List<Point3d> hullPoints = new List<Point3d>();
                    foreach (int pIndex in node.PlateIndices) hullPoints.AddRange(lattice.Plates[pIndex].Vtc);
                    MeshTools.ConvexHull(hullPoints, sides, out hullMesh);

                    // Remove plate faces
                    List<int> deleteFaces = new List<int>();
                    foreach (int plateIndx in node.PlateIndices)
                    {
                        List<Point3f> plateVtc;
                        MeshTools.Point3dToPoint3f(lattice.Plates[plateIndx].Vtc, out plateVtc);
                        // recall that strut plates have 'sides+1' vertices.
                        // if the plate has only 'sides' vertices, it is an extra plate (for acute nodes), so we should keep it
                        if (plateVtc.Count < sides + 1) continue;

                        for (int j = 0; j < hullMesh.Faces.Count; j++)
                        {
                            Point3f ptA, ptB, ptC, ptD;
                            hullMesh.Faces.GetFaceVertices(j, out ptA, out ptB, out ptC, out ptD);

                            // check if the mesh face has vertices that belong to a single plate, if so we need to remove the face
                            int matches = 0;
                            foreach (Point3f testPt in plateVtc)
                                if (testPt.EpsilonEquals(ptA, (float)tol) || testPt.EpsilonEquals(ptB, (float)tol) || testPt.EpsilonEquals(ptC, (float)tol))
                                    matches++;
                            // if matches == 3, we should remove the face
                            if (matches == 3)
                                deleteFaces.Add(j);
                        }
                    }
                    deleteFaces.Reverse();
                    foreach (int faceIndx in deleteFaces) hullMesh.Faces.RemoveAt(faceIndx);

                    outMesh.Append(hullMesh);
                    //hullMeshList.Add(hullMesh);
                }
            }

            // POST-PROCESS FINAL MESH
            outMesh.Vertices.CombineIdentical(true, true);
            outMesh.FaceNormals.ComputeFaceNormals();
            outMesh.UnifyNormals();
            outMesh.Normals.ComputeNormals();


            DA.SetData(0, outMesh);
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
                //return Exoskeleton.Properties.Resources.exoskel;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{a51ac688-3afc-48a5-b121-48cecf687eb5}"); }
        }
        
    }
}




