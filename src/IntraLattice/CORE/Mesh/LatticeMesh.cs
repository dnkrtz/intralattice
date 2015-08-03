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
            pManager.AddCurveParameter("Struts", "Struts", "Curve network", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius (start)", "Rs", "List of radii for start of struts", GH_ParamAccess.list, 0.6);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Lattice Mesh", GH_ParamAccess.item);
            pManager.AddMeshParameter("Hulls", "V", "Lattice Mesh Vertices", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables
            List<Curve> inputStruts = new List<Curve>();
            List<double> radiusList = new List<double>();

            // Attempt to fetch data inputs
            if (!DA.GetDataList(0, inputStruts)) { return; }
            if (!DA.GetDataList(1, radiusList)) { return; }

            // Validate data
            if (inputStruts == null || inputStruts.Count == 0) { return; }
            if (radiusList == null || radiusList.Count == 0 || radiusList.Contains(0)) { return; }

            // Set some variables
            int sides = 6;  // Number of sides on each strut
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            bool latticeIsLinear = true;    // will become false if a non-linear curve is found in the strut list

            // Initialize lattice object
            Lattice lattice = new Lattice();
            // Initialize output mesh
            Mesh outMesh = new Mesh();

            //====================================================================================
            // STEP 1 - Data structure
            // In this section, we construct the wireframe lattice
            // Ensuring that no duplicate nodes or struts are present
            //====================================================================================

            // These two lookup lists are used to determine if the nodes or pairs of nodes are already defined
            // Such that we avoid duplicates
            Point3dList nodeLookup = new Point3dList();
            List<IndexPair> nodePairLookup = new List<IndexPair>();

            // Loop over list of struts
            for (int i = 0; i < inputStruts.Count; i++ )
            {
                Curve strut = inputStruts[i];
                // if strut is invalid, skip it
                if (strut == null || !strut.IsValid) continue;

                // We must ignore duplicate nodes
                Point3d[] nodes = new Point3d[2] { strut.PointAtStart, strut.PointAtEnd };
                List<int> nodeIndices = new List<int>();
                // Loop over end points of strut
                // Check if node is already in nodeLookup list, if so, we find its index instead of creating a new node
                for (int j = 0; j < 2; j++ )
                {
                    Point3d pt = nodes[j];
                    int closestIndex = nodeLookup.ClosestIndex(pt);  // find closest node to current pt

                    // If node already exists (within tolerance), set the index
                    if (nodeLookup.Count != 0 && pt.EpsilonEquals(nodeLookup[closestIndex], tol))
                        nodeIndices.Add(closestIndex);
                    // If node doesn't exist
                    else
                    {
                        // update lookup list
                        nodeLookup.Add(pt);
                        // construct node
                        lattice.Nodes.Add(new Node(pt));
                        nodeIndices.Add(nodeLookup.Count - 1);
                    }
                }

                // We must ignore duplicate struts
                IndexPair nodePair = new IndexPair(nodeIndices[0], nodeIndices[1]);
                // So we only create the strut if it doesn't exist yet (check nodePairLookup list)
                if (nodePairLookup.Count == 0 || !nodePairLookup.Contains(nodePair))
                {
                    // update the lookup list
                    nodePairLookup.Add(nodePair);
                    // construct strut
                    lattice.Struts.Add(new Strut(strut, nodePair));
                    int strutIndex = lattice.Struts.Count - 1;
                    // construct plates
                    lattice.Plates.Add(new Plate(nodeIndices[0], strut.TangentAtStart));
                    lattice.Plates.Add(new Plate(nodeIndices[1], - strut.TangentAtEnd));
                    // set strut relational parameters
                    IndexPair platePair = new IndexPair(lattice.Plates.Count - 2, lattice.Plates.Count - 1);
                    lattice.Struts[strutIndex].PlatePair = platePair;
                    // set node relational parameters
                    lattice.Nodes[nodeIndices[0]].StrutIndices.Add(strutIndex);
                    lattice.Nodes[nodeIndices[1]].StrutIndices.Add(strutIndex);
                    lattice.Nodes[nodeIndices[0]].PlateIndices.Add(platePair.I);
                    lattice.Nodes[nodeIndices[1]].PlateIndices.Add(platePair.J);
                }

                // if lattice is thought to be linear, but the curve is not linear, update boolean
                if (latticeIsLinear && !strut.IsLinear()) latticeIsLinear = false;
            }


            //====================================================================================
            // STEP 2 - Compute nodal radii
            // Strut radius is node-based
            //====================================================================================

            // Loop over nodes
            foreach (Node node in lattice.Nodes)
            {
                node.Radius = radiusList[0];
            }


            //====================================================================================
            // STEP 3 - Compute plate offsets
            // Each plate is offset from its parent node, to avoid mesh overlaps.
            // Uses simple trig to compute the offset requried.
            //====================================================================================

            // Loop over nodes
            for (int i = 0; i < lattice.Nodes.Count; i++)
            {
                Node node = lattice.Nodes[i];   // the node being evaluated
                bool isAcute = true;   // true if all pairs of struts form acute angles with eachother

                if (node.StrutIndices.Count < 2) continue;

                // Loop over all possible pairs of plates on the node
                // This automatically avoids setting offsets for nodes with a single strut
                for (int j = 0; j < node.StrutIndices.Count; j++)
                {
                    for (int k = j + 1; k < node.StrutIndices.Count; k++)
                    {
                        Strut strutA = lattice.Struts[node.StrutIndices[j]];
                        Strut strutB = lattice.Struts[node.StrutIndices[k]];
                        Plate plateA = lattice.Plates[node.PlateIndices[j]];
                        Plate plateB = lattice.Plates[node.PlateIndices[k]];
                        
                        // if linear struts
                        if (strutA.Curve.IsLinear(tol) && strutB.Curve.IsLinear(tol))
                        {
                            // compute the angle between the struts
                            double theta = Vector3d.VectorAngle(plateA.Normal, plateB.Normal);
                            // if angle is a reflex angle (angle greater than 180deg), we need to adjust it
                            if (theta > Math.PI) theta = 2 * Math.PI - theta;

                            double offset = 0;

                            // if angle is greater than 90deg, simple case: offset is based on radius at node
                            if (theta > Math.PI / 2)
                            {
                                // if we set it equal exactly to the node radius
                                // the convex hull is much more complex to clean, since some vertices might lie on the plane of other plates
                                // so we increase by 5% for robustness
                                offset = node.Radius * 1.05;
                            }
                            // if angle is acute, we need some simple trig
                            else
                                offset = node.Radius / (Math.Sin(theta / 2.0));

                            // if offset is greater than previously set offset, adjust
                            if (offset > plateA.Offset)
                                plateA.Offset = offset;
                            if (offset > plateB.Offset)
                                plateB.Offset = offset;
                        }
                        // if curved struts
                        else
                        {
                            plateA.Offset = 1.1;
                            plateB.Offset = 1.1;
                        }

                    }
                }

                Vector3d extraNormal = new Vector3d();
                foreach (int plateIndex in node.PlateIndices)
                {
                    extraNormal += lattice.Plates[plateIndex].Normal;
                }
                foreach (int plateIndex in node.PlateIndices)
                {
                    if (Vector3d.VectorAngle(-extraNormal, lattice.Plates[plateIndex].Normal) < Math.PI/2) isAcute = false;
                }

                //  for better mesh shape at sharp corners, add an extra plate if the strut set is acute
                if (isAcute)
                {

                    List<Point3d> Vtc;
                    Plane plane = new Plane(node.Point3d - extraNormal/6, -extraNormal);
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
            // STEP 4 - Construct sleeve meshes and hull points
            // 
            //====================================================================================

            // Loop over struts
            for (int i = 0; i < lattice.Struts.Count; i++)
            {
                Mesh sleeveMesh = new Mesh();

                Strut strut = lattice.Struts[i];
                Plate startPlate = lattice.Plates[strut.PlatePair.I];   // plate for the start of the strut
                Plate endPlate = lattice.Plates[strut.PlatePair.J];
                double startParam, endParam;
                strut.Curve.Domain = new Interval(0, 1);
                strut.Curve.LengthParameter(startPlate.Offset, out startParam);   // get start and end params of strut (accounting for offset)
                strut.Curve.LengthParameter(strut.Curve.GetLength() - endPlate.Offset, out endParam);
                startPlate.Vtc.Add(strut.Curve.PointAt(startParam));    // set center point of star & end plates
                endPlate.Vtc.Add(strut.Curve.PointAt(endParam));
                double startRadius = lattice.Nodes[strut.NodePair.I].Radius;    // set radius at start & end
                double endRadius = lattice.Nodes[strut.NodePair.J].Radius;

                // compute the number of divisions
                double avgRadius = (startRadius + endRadius) / 2;
                double length = strut.Curve.GetLength(new Interval(startParam, endParam));
                double divisions = Math.Max((Math.Round(length * 0.5 / avgRadius) * 2), 2); // Number of sleeve divisions (must be even)

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
                        double R = startRadius - j / (double)divisions * (startRadius - endRadius); //variable radius
                        double startAngle = j * Math.PI / sides; // this twists the plate points along the strut, for triangulation
                        
                        List<Point3d> Vtc;
                        MeshTools.CreatePlate(plane, sides, R, startAngle, out Vtc);    // compute the vertices

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
                        double locParameter = startParam + (j / divisions)*(endParam-startParam);

                        Point3d knucklePt = strut.Curve.PointAt(locParameter);
                        Plane plane;
                        strut.Curve.PerpendicularFrameAt(locParameter, out plane);
                        double R = startRadius - j / (double)divisions * (startRadius - endRadius); //variable radius
                        double startAngle = j * Math.PI / sides; // this twists the plate points along the strut, for triangulation

                        List<Point3d> Vtc;
                        MeshTools.CreatePlate(plane, sides, R, startAngle, out Vtc);    // compute the vertices

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
                    //{
                    //    // recall that strut plates have 'sides+1' vertices.
                    //    // if the plate has only 'sides' vertices, it is an extra plate (for acute nodes), so we should keep it
                    //    if (lattice.Plates[plateIndx].Vtc.Count < sides + 1) continue;

                    //    // now, set the plate normals
                    //    Vector3d pNormal = lattice.Plates[plateIndx].Normal;
                    //    Vector3f plateNormal = new Vector3f((float)pNormal.X, (float)pNormal.Y, (float)pNormal.Z);
                    //    // we remove any face that has the same face normal as a plate
                    //    for (int j = 0; j < hullMesh.Faces.Count; j++)
                    //    {
                    //        if (hullMesh.FaceNormals[j].EpsilonEquals(plateNormal, (float)tol))
                    //            deleteFaces.Add(j);
                    //    }
                    //}
                    {
                        List<Point3f> plateVtc;
                        MeshTools.Point3dToPoint3f(lattice.Plates[plateIndx].Vtc, out plateVtc);

                        if (plateVtc.Count < sides + 1) continue;

                        for (int j = 0; j < hullMesh.Faces.Count; j++)
                        {
                            Point3f ptA, ptB, ptC, ptD;
                            hullMesh.Faces.GetFaceVertices(j, out ptA, out ptB, out ptC, out ptD);
                            
                            int matches = 0; // if equal to 3, meshface vtc are all plate vtc, and we should remove the face

                            foreach (Point3f testPt in plateVtc)
                            {
                                if (testPt.EpsilonEquals(ptA, (float)tol) || testPt.EpsilonEquals(ptB, (float)tol) || testPt.EpsilonEquals(ptC, (float)tol)) 
                                    matches++;
                            }

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
            DA.SetDataList(1, hullMeshList);

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
