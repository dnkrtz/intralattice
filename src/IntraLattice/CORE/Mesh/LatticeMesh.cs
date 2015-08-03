using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino;
using IntraLattice.Properties;
using Rhino.Geometry.Intersect;

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
            // STEP 1 - Network cleanse
            // Clean the network of curves by:
            // - Removing duplicate nodes and struts
            // - Combining colinear struts
            //====================================================================================
            
            // These lookup lists are used to determine if the nodes or pairs of nodes are already defined
            // Such that we avoid duplicates
            Point3dList nodeLookup = new Point3dList();
            List<IndexPair> nodePairLookup = new List<IndexPair>();
            List<Curve> strutLookup = new List<Curve>();

            MeshTools.CleanNetwork(inputStruts, out nodeLookup, out nodePairLookup, out strutLookup);

            //====================================================================================
            // STEP 1 - Data structure
            // In this section, we construct the wireframe lattice
            // Ensuring that no duplicate nodes or struts are present
            //====================================================================================


            // Create nodes
            foreach (Point3d node in nodeLookup)
                lattice.Nodes.Add(new Node(node));

            // Create struts and plates
            for (int i = 0; i < strutLookup.Count; i++ )
            {
                lattice.Struts.Add(new Strut(strutLookup[i], nodePairLookup[i])); // assign
                // construct plates
                lattice.Plates.Add(new Plate(nodePairLookup[i].I, strutLookup[i].TangentAtStart));
                lattice.Plates.Add(new Plate(nodePairLookup[i].J, - strutLookup[i].TangentAtEnd));
                // set strut relational parameters
                IndexPair platePair = new IndexPair(lattice.Plates.Count - 2, lattice.Plates.Count - 1);
                lattice.Struts[i].PlatePair = platePair;
                // set node relational parameters
                lattice.Nodes[nodePairLookup[i].I].StrutIndices.Add(i);
                lattice.Nodes[nodePairLookup[i].J].StrutIndices.Add(i);
                lattice.Nodes[nodePairLookup[i].I].PlateIndices.Add(platePair.I);
                lattice.Nodes[nodePairLookup[i].J].PlateIndices.Add(platePair.J);
                
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
                
                if (node.StrutIndices.Count < 2) continue;

                // Compute the offset required
                double offset;
                MeshTools.ComputeOffsets(node, lattice, tol, out offset);

                // Determine if the struts at the node form a 'sharp' corner
                bool isAcute = true;
                Vector3d extraNormal = new Vector3d();
                foreach (int plateIndex in node.PlateIndices)
                    extraNormal += lattice.Plates[plateIndex].Normal;
                foreach (int plateIndex in node.PlateIndices)
                    if (Vector3d.VectorAngle(-extraNormal, lattice.Plates[plateIndex].Normal) < Math.PI/2)
                        isAcute = false;

                //  If struts form a sharp corner, add an extra plate for a better convex hull shape
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
