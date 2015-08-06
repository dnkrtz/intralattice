using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;
using Rhino.Geometry.Intersect;
using IntraLattice.CORE.MeshModule.Data;

namespace IntraLattice.CORE.MeshModule
{

    public class LatticeMesh
    {
        /// <summary>
        /// Public constructor
        /// </summary>
        public LatticeMesh()
        {
            this.Nodes = new List<Node>();
            this.Struts = new List<Strut>();
            this.Plates = new List<Plate>();
            this.Mesh = new Mesh();
        }
        
        // Properties
        //
        /// <summary>
        /// List of nodes in the lattice (as Node objects).
        /// </summary>
        public List<Node> Nodes { get; set; }
        /// <summary>
        /// List of struts in the lattice (as Strut objects).
        /// </summary>
        public List<Strut> Struts { get; set; }
        /// <summary>
        /// List of plates in the lattice (as Plate objects).
        /// </summary>
        /// <remarks>
        /// Plates are essentially the vertices that are shared between sleeve and hull meshes.
        /// </remarks>
        public List<Plate> Plates { get; set; }
        /// <summary>
        /// The actual mesh of the lattice.
        /// </summary>
        public Mesh Mesh { get; set; }

        // Pre-processsing methods
        //
        /// <summary>
        /// Adds a plate to the node if it is a 'sharp' node, to improve convex hull shape.
        /// </summary>
        /// <param name="nodeIndex"> Index of the node we want to check/fix. </param>
        /// <param name="sides"> Number of sides on the sleeve meshes. </param>
        public void FixSharpNodes(int nodeIndex, int sides)
        {
            Node node = this.Nodes[nodeIndex];

            // The extra plate is in the direction of the negative sum of all normals
            // We use the new plate normal to check if the node struts are contained within a 180deg peripheral (i.e. the node is 'sharp')
            bool isSharp = true;
            Vector3d extraNormal = new Vector3d();  // sum of all normals
            foreach (int plateIndex in node.PlateIndices)
                extraNormal += this.Plates[plateIndex].Normal;
            foreach (int plateIndex in node.PlateIndices)
                if (Vector3d.VectorAngle(-extraNormal, this.Plates[plateIndex].Normal) < Math.PI / 2)
                    isSharp = false;

            //  If struts form a sharp corner, add an extra plate for a better convex hull shape
            if (isSharp)
            {
                
                // plane offset from node slightly
                Plane plane = new Plane(node.Point3d - extraNormal * node.Radius / 3, -extraNormal);
                List<Point3d> Vtc = MeshTools.CreateKnuckle(plane, sides, node.Radius, 0);    // compute the vertices
                // add new plate and its vertices
                this.Plates.Add(new Plate(nodeIndex, -extraNormal));
                int newPlateIndx = this.Plates.Count - 1;
                this.Plates[newPlateIndx].Vtc.AddRange(Vtc);
                node.PlateIndices.Add(newPlateIndx);
            }
        }
        /// <summary>
        /// Computes plate offsets required to avoid mesh overlaps.
        /// For linear struts, this is done with simple trig.
        /// For curved struts, sphere intersections are used.
        /// </summary>
        /// <param name="nodeIndex">Index of the node who's plates we are computing offsets for.</param>
        /// <param name="tol">Tolerance for point locations (RhinoDoc.ActiveDoc.ModelAbsoluteTolerance is a good bet).</param>
        /// <returns>True if offsets are valid, false if struts are engulfed by their nodes.</returns>
        public bool ComputeOffsets(int nodeIndex, double tol)
        {
            Node node = this.Nodes[nodeIndex];
            // the minimum offset is based on the radius at the node
            // if equal to the radius, the convex hull is much more complex to clean, since some vertices might lie on the plane of other plates
            // so we increase by 5% for robustness
            double minOffset = node.Radius * 1.05;
            double testOffset = minOffset;
            List<double> offsets = new List<double>() {testOffset};

            // Loop over all possible pairs of plates on the node
            for (int j = 0; j < node.StrutIndices.Count; j++)
            {
                for (int k = j + 1; k < node.StrutIndices.Count; k++)
                {
                    Strut strutA = this.Struts[node.StrutIndices[j]];
                    Strut strutB = this.Struts[node.StrutIndices[k]];
                    Plate plateA = this.Plates[node.PlateIndices[j]];
                    Plate plateB = this.Plates[node.PlateIndices[k]];

                    double maxOffset = Math.Min(strutA.Curve.GetLength(), strutB.Curve.GetLength());

                    // if linear struts
                    if (strutA.Curve.IsLinear(tol) && strutB.Curve.IsLinear(tol))
                    {
                        // compute the angle between the struts
                        double theta = Vector3d.VectorAngle(plateA.Normal, plateB.Normal);
                        // if angle is a reflex angle (angle greater than 180deg), we need to adjust it
                        if (theta > Math.PI) theta = 2 * Math.PI - theta;

                        // if angle is greater than 90deg, simple case: offset is based on radius at node
                        if (theta > Math.PI / 2)
                            testOffset = minOffset;
                        // if angle is acute, we need some simple trig
                        else
                            testOffset = node.Radius*1.05 / (Math.Sin(theta / 2.0));
                    }
                    // if curved struts
                    else
                    {
                        // The curves we'll work with
                        Curve curveA = strutA.Curve.DuplicateCurve();
                        Curve curveB = strutB.Curve.DuplicateCurve();

                        // May need to reverse direction
                        if (curveA.PointAtEnd.EpsilonEquals(node.Point3d, tol))
                            curveA.Reverse();
                        if (curveB.PointAtEnd.EpsilonEquals(node.Point3d, tol))
                            curveB.Reverse();

                        // Now perform incremental offset
                        for (testOffset = minOffset; testOffset < maxOffset; testOffset += minOffset / 5)
                        {
                            Sphere sphereA = new Sphere(curveA.PointAtLength(testOffset), node.Radius);
                            Sphere sphereB = new Sphere(curveB.PointAtLength(testOffset), node.Radius);

                            // Check for intersection of the two spheres. If none found, we're good to go.
                            Circle intersectLine;
                            if (Intersection.SphereSphere(sphereA, sphereB, out intersectLine) == 0) break;
                        }
                        testOffset += minOffset / 5;
                    }

                    // if offset greater than length of strut, it is engulfed
                    if (testOffset > maxOffset)
                        return false;
                    // if offset is greater than previously set offset, but not almost equal, adjust
                    double offset;
                    bool offsetFound = MeshTools.FilterOffset(testOffset, offsets, tol*100*node.Radius, out offset);
                    if (!offsetFound)
                        offsets.Add(offset);
                    if (offset > plateA.Offset)
                        plateA.Offset = offset;
                    if (offset > plateB.Offset)
                        plateB.Offset = offset;
                        
                }
            }

            foreach (int plateIndx in node.PlateIndices)
            {

            }

            return true;
        }

        // Meshing methods
        //
        /// <summary>
        /// Generates sleeve mesh for the struts. The plate offsets should be set before you use this method.
        /// </summary>
        /// <param name="strutIndex">Index of the strut being thickened.</param>
        /// <param name="sides">Number of sides for the strut mesh.</param>
        /// <param name="sleeveMesh">The sleeve mesh</param>
        public Mesh MakeSleeve(int strutIndex, int sides)
        {
            Mesh sleeveMesh = new Mesh();
            Strut strut = this.Struts[strutIndex];
            Plate startPlate = this.Plates[strut.PlatePair.I];   // plate for the start of the strut
            Plate endPlate = this.Plates[strut.PlatePair.J];
            double startParam, endParam;
            strut.Curve.LengthParameter(startPlate.Offset, out startParam);   // get start and end params of strut (accounting for offset)
            strut.Curve.LengthParameter(strut.Curve.GetLength() - endPlate.Offset, out endParam);
            double startRadius = this.Nodes[strut.NodePair.I].Radius;    // set radius at start & end
            double endRadius = this.Nodes[strut.NodePair.J].Radius;

            // set center point of start & end plates
            startPlate.Vtc.Add(strut.Curve.PointAt(startParam));
            endPlate.Vtc.Add(strut.Curve.PointAt(endParam));

            // compute the number of sleeve divisions
            double avgRadius = (startRadius + endRadius) / 2;
            double length = strut.Curve.GetLength(new Interval(startParam, endParam));
            double divisions = Math.Max((Math.Round(length * 0.5 / avgRadius) * 2), 2); // Number of sleeve divisions (must be even)

            // GENERATE SLEEVE VERTICES

            Vector3d normal = strut.Curve.TangentAtStart;

            // Loops: j along strut
            for (int j = 0; j <= divisions; j++)
            {
                Plane plane;
                if (strut.Curve.IsLinear()) // for linear strut
                {
                    Point3d knucklePt = startPlate.Vtc[0] + (normal * (length * j / divisions));
                    plane = new Plane(knucklePt, normal);
                }
                else // for curved struts, we compute a new perpendicular frame at every iteration
                {
                    double locParameter = startParam + (j / divisions) * (endParam - startParam);
                    Point3d knucklePt = strut.Curve.PointAt(locParameter);
                    strut.Curve.PerpendicularFrameAt(locParameter, out plane);
                }
                double R = startRadius - j / (double)divisions * (startRadius - endRadius); //variable radius
                double startAngle = j * Math.PI / sides; // this twists the plate points along the strut, for triangulation

                List<Point3d> Vtc = MeshTools.CreateKnuckle(plane, sides, R, startAngle);    // compute the vertices

                // if the vertices are hull points (plates that connect sleeves to node hulls), save them
                if (j == 0) startPlate.Vtc.AddRange(Vtc);
                if (j == divisions) endPlate.Vtc.AddRange(Vtc);

                sleeveMesh.Vertices.AddVertices(Vtc); // save vertices to sleeve mes
            }

            // STITCH SLEEVE FACES

            int V1, V2, V3, V4;
            for (int j = 0; j < divisions; j++)
            {
                for (int i = 0; i < sides; i++)
                {
                    V1 = (j * sides) + i;
                    V2 = (j * sides) + i + sides;
                    V3 = (j * sides) + sides + (i + 1) % (sides);
                    V4 = (j * sides) + (i + 1) % (sides);

                    sleeveMesh.Faces.AddFace(V1, V2, V4);
                    sleeveMesh.Faces.AddFace(V2, V3, V4);
                }
            }

            return sleeveMesh;
        }
        /// <summary>
        /// Generates a convex hull mesh for a set of points.
        /// </summary>
        /// <param name="nodeIndex">Index of node being hulled.</param>
        /// <param name="sides">Number of sides per strut.</param>
        /// <param name="tol">The tolerance (RhinoDoc.ActiveDoc.ModelAbsoluteTolerance is a good bet).</param>
        /// <param name="cleanPlates">If true, the plate faces will be removed from the hull, so that the sleeves can be directly attached.</param>
        /// <remarks>
        /// If a plate point is coplanar with another plate, the hull may be impossible to clean.
        /// This is because the hulling process may remove the coplanar point and create a new face.
        /// </remarks>
        public Mesh MakeConvexHull(int nodeIndex, int sides, double tol, bool cleanPlates)
        {
            Mesh hullMesh = new Mesh();
            Node node = this.Nodes[nodeIndex];
            double radius = node.Radius;

            double planeTolerance = tol * radius / 10;

            // Collect all hull points (i.e. all plate points at the node)
            List<Point3d> pts = new List<Point3d>();
            foreach (int pIndex in node.PlateIndices) pts.AddRange(this.Plates[pIndex].Vtc);

            // 1. Create initial tetrahedron.
            // Form triangle from 3 first points (lie on same plate, thus, same plane)
            hullMesh.Vertices.Add(pts[0]);
            hullMesh.Vertices.Add(pts[1]);
            hullMesh.Vertices.Add(pts[2]);
            Plane planeStart = new Plane(pts[0], pts[1], pts[2]);
            // Form tetrahedron with a 4th point which does not lie on the same plane
            int nextIndex = sides + 1;
            while (Math.Abs(planeStart.DistanceTo(pts[nextIndex])) < planeTolerance)
                nextIndex++;
            hullMesh.Vertices.Add(pts[nextIndex]);
            // Stitch faces of tetrahedron
            hullMesh.Faces.AddFace(0, 2, 1);
            hullMesh.Faces.AddFace(0, 3, 2);
            hullMesh.Faces.AddFace(0, 1, 3);
            hullMesh.Faces.AddFace(1, 2, 3);

            // 2. Begin the incremental hulling process
            // Remove points already checked
            pts.RemoveAt(nextIndex);
            pts.RemoveRange(0, 3);

            // Loop through the remaining points
            for (int i = 0; i < pts.Count; i++)
            {
                MeshTools.NormaliseMesh(ref hullMesh);

                // Find visible faces
                List<int> seenFaces = new List<int>();
                for (int faceIndex = 0; faceIndex < hullMesh.Faces.Count; faceIndex++)
                {
                    Vector3d testVect = pts[i] - hullMesh.Faces.GetFaceCenter(faceIndex);
                    double angle = Vector3d.VectorAngle(hullMesh.FaceNormals[faceIndex], testVect);
                    Plane planeTest = new Plane(hullMesh.Faces.GetFaceCenter(faceIndex), hullMesh.FaceNormals[faceIndex]);
                    if (angle < Math.PI * 0.5 || Math.Abs(planeTest.DistanceTo(pts[i])) < planeTolerance) { seenFaces.Add(faceIndex); }
                }

                // Remove visible faces
                hullMesh.Faces.DeleteFaces(seenFaces);
                // Add current point
                hullMesh.Vertices.Add(pts[i]);

                List<MeshFace> addFaces = new List<MeshFace>();
                // Close open hull with new vertex
                for (int edgeIndex = 0; edgeIndex < hullMesh.TopologyEdges.Count; edgeIndex++)
                {
                    if (!hullMesh.TopologyEdges.IsSwappableEdge(edgeIndex))
                    {
                        IndexPair V = hullMesh.TopologyEdges.GetTopologyVertices(edgeIndex);
                        int I1 = hullMesh.TopologyVertices.MeshVertexIndices(V.I)[0];
                        int I2 = hullMesh.TopologyVertices.MeshVertexIndices(V.J)[0];
                        addFaces.Add(new MeshFace(I1, I2, hullMesh.Vertices.Count - 1));
                    }
                }
                hullMesh.Faces.AddFaces(addFaces);
            }

            MeshTools.NormaliseMesh(ref hullMesh);


            // If requested, delete the hull faces that lie on the plates (so sleeves can connect directly to the hulls)
            if (cleanPlates)
            {
                List<int> deleteFaces = new List<int>();
                foreach (int plateIndx in node.PlateIndices)
                {
                    List<Point3f> plateVtc = MeshTools.Point3dToPoint3f(this.Plates[plateIndx].Vtc);
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
            }
            return hullMesh;
        }
        /// <summary>
        /// Construts endface mesh (single strut nodes).
        /// </summary>
        /// <param name="nodeIndex">Index of the node where the endface should be generated.</param>
        /// <param name="sides">Number of strut sides.</param>
        public Mesh MakeEndFace(int nodeIndex, int sides)
        {
            Mesh endMesh = new Mesh();
            // Set vertices
            foreach (Point3d platePoint in this.Plates[this.Nodes[nodeIndex].PlateIndices[0]].Vtc)
                endMesh.Vertices.Add(platePoint);
            // Stitch faces
            for (int i = 1; i < sides; i++)
                endMesh.Faces.AddFace(0, i, i + 1);
            endMesh.Faces.AddFace(0, sides, 1); // last face wraps*/

            return endMesh;
        }
    }
}
