using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This is a set of methods & objects used by the mesh components
// =====================================================
// 
// ConvexHull -> Implements a 3D convex hull algorithm that assumes all points lie on the hull (which is our case)
// NormaliseMesh -> Adjusts orientation of face normals 
// SleeveStitch -> Constructs the sleeve mesh faces (stitches the vertices)
// EndFaceStitch -> Constructs the endface mesh faces (needed for single strut nodes)

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class MeshTools
    {

        public static void CleanNetwork(List<Curve> inputStruts, out Point3dList nodes, out List<IndexPair> nodePairs, out List<Curve> struts)
        {
            nodes = new Point3dList();
            nodePairs = new List<IndexPair>();
            struts = new List<Curve>();

            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Loop over list of struts
            for (int i = 0; i < inputStruts.Count; i++)
            {
                Curve strut = inputStruts[i];
                // if strut is invalid, ignore it
                if (strut == null || !strut.IsValid || strut.IsShort(100*tol)) continue;

                // We must ignore duplicate nodes
                Point3d[] pts = new Point3d[2] { strut.PointAtStart, strut.PointAtEnd };
                List<int> nodeIndices = new List<int>();
                // Loop over end points of strut
                // Check if node is already in nodeLookup list, if so, we find its index instead of creating a new node
                for (int j = 0; j < 2; j++)
                {
                    Point3d pt = pts[j];
                    int closestIndex = nodes.ClosestIndex(pt);  // find closest node to current pt

                    // If node already exists (within tolerance), set the index
                    if (nodes.Count != 0 && pt.EpsilonEquals(nodes[closestIndex], tol))
                        nodeIndices.Add(closestIndex);
                    // If node doesn't exist
                    else
                    {
                        // update lookup list
                        nodes.Add(pt);
                        nodeIndices.Add(nodes.Count - 1);
                    }
                }

                // We must ignore duplicate struts
                IndexPair nodePair = new IndexPair(nodeIndices[0], nodeIndices[1]);
                // So we only create the strut if it doesn't exist yet (check nodePairLookup list)
                if (nodePairs.Count == 0 || !nodePairs.Contains(nodePair))
                {
                    // update the lookup list
                    nodePairs.Add(nodePair);
                    strut.Domain = new Interval(0, 1);
                    struts.Add(strut);
                }
            }
        }

        public static void StrutPairOffset(Node node, Lattice lattice, double tol, out double offset)
        {
            // the minimum offset is based on the radius at the node
            // if equal to the radius, the convex hull is much more complex to clean, since some vertices might lie on the plane of other plates
            // so we increase by 5% for robustness
            double minOffset = node.Radius * 1.15;
            offset = minOffset;

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
                            offset = minOffset;
                        // if angle is acute, we need some simple trig
                        else
                            offset = node.Radius / (Math.Sin(theta / 2.0));


                    }
                    // if curved struts
                    else
                    {
                        // The curves we'll work with
                        Curve curveA = strutA.Curve.DuplicateCurve();
                        Curve curveB = strutB.Curve.DuplicateCurve();

                        // May need to reverse direction
                        if (strutA.Curve.PointAtEnd.EpsilonEquals(node.Point3d, tol))
                            curveA.Reverse();
                        if (strutB.Curve.PointAtEnd.EpsilonEquals(node.Point3d, tol))
                            curveB.Reverse();

                        // Now perform incremental offset
                        for (offset = minOffset; offset < maxOffset; offset += minOffset / 5)
                        {

                            Sphere sphereA = new Sphere(curveA.PointAtLength(offset), node.Radius);
                            Sphere sphereB = new Sphere(curveB.PointAtLength(offset), node.Radius);

                            // Intersect the two planes
                            Circle intersectLine;
                            if (Intersection.SphereSphere(sphereA, sphereB, out intersectLine) == 0) break;

                        }
                    }

                    // if offset is greater than previously set offset, adjust
                    if (offset > plateA.Offset)
                        plateA.Offset = offset;
                    if (offset > plateB.Offset)
                        plateB.Offset = offset;
                }
            }
        }


        public static void CreatePlate(Plane plane, int sides, double radius, double startAngle, out List<Point3d> Vtc)
        {
            Vtc = new List<Point3d>();

            // this loop rotates around the strut, creating vertices
            for (int k = 0; k < sides; k++)
            {
                double angle = k * 2 * Math.PI / sides + startAngle;
                Vtc.Add(plane.PointAt(radius * Math.Cos(angle), radius * Math.Sin(angle))); // create vertex
            }
        }

        public static void Point3dToPoint3f(List<Point3d> in3d, out List<Point3f> out3f)
        {
            out3f = new List<Point3f>();

            foreach (Point3d pt3d in in3d)
            {
                out3f.Add(new Point3f((float)pt3d.X, (float)pt3d.Y, (float)pt3d.Z));
            }
        }

        /// <summary>
        /// Incremental 3D convex hull algorithm
        /// </summary>
        public static void ConvexHull(List<Point3d> pts, int sides, out Mesh hullMesh)
        {
            hullMesh = new Mesh();
            int totalPts = pts.Count;

            // 1. Create initial tetrahedron.
            // Form triangle from 3 first points (lie on same plate, thus, same plane)
            hullMesh.Vertices.Add(pts[0]);
            hullMesh.Vertices.Add(pts[1]);
            hullMesh.Vertices.Add(pts[2]);
            Plane PlaneStart = new Plane(pts[0], pts[1], pts[2]);
            // Form tetrahedron with a 4th point which does not lie on the same plane
            // Point S+1 is the centerpoint of another plate, therefore it is surely on a different plane.
            hullMesh.Vertices.Add(pts[sides + 2]);
            // Stitch faces of tetrahedron
            hullMesh.Faces.AddFace(0, 2, 1);
            hullMesh.Faces.AddFace(0, 3, 2);
            hullMesh.Faces.AddFace(0, 1, 3);
            hullMesh.Faces.AddFace(1, 2, 3);

            // 2. Begin the incremental hulling process
            // Remove points already checked
            pts.RemoveAt(sides + 1);
            pts.RemoveRange(0, 3);
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;


            // Loop through the remaining points
            for (int i = 0; i < pts.Count; i++)
            {
                NormaliseMesh(ref hullMesh);

                // Find visible faces
                List<int> seenFaces = new List<int>();
                for (int faceIndex = 0; faceIndex < hullMesh.Faces.Count; faceIndex++)
                {
                    Vector3d testVect = pts[i] - hullMesh.Faces.GetFaceCenter(faceIndex);
                    double angle = Vector3d.VectorAngle(hullMesh.FaceNormals[faceIndex], testVect);
                    Plane planeTest = new Plane(hullMesh.Faces.GetFaceCenter(faceIndex), hullMesh.FaceNormals[faceIndex]);
                    if (angle < Math.PI * 0.5 || Math.Abs(planeTest.DistanceTo(pts[i])) < tol/100) { seenFaces.Add(faceIndex); }
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

            NormaliseMesh(ref hullMesh);

        }

        /// <summary>
        /// Fix orientation of face normals
        /// </summary>
        public static void NormaliseMesh(ref Mesh mesh)
        {
            if (mesh.SolidOrientation() == -1) mesh.Flip(true, true, true);
            mesh.FaceNormals.ComputeFaceNormals();
            mesh.UnifyNormals();
            mesh.Normals.ComputeNormals();
        }

        /// <summary>
        /// Constructs sleeve mesh faces (stitches the vertices)
        /// </summary>
        public static void SleeveStitch(ref Mesh strutMesh, double divisions, int sides)
        {
            int V1, V2, V3, V4;
            for (int j = 0; j < divisions; j++)
            {
                for (int i = 0; i < sides; i++)
                {
                    V1 = (j * sides) + i;
                    V2 = (j * sides) + i + sides;
                    V3 = (j * sides) + sides + (i + 1) % (sides);
                    V4 = (j * sides) + (i + 1) % (sides);

                    strutMesh.Faces.AddFace(V1, V2, V4);
                    strutMesh.Faces.AddFace(V2, V3, V4);
                }
            }
        }

        /// <summary>
        /// Construts endface mesh (single strut nodes)
        /// </summary>
        public static void EndFaceStitch(ref Mesh endMesh, int sides)
        {
            // Stitch faces
            for (int i = 1; i < sides; i++) endMesh.Faces.AddFace(0, i, i + 1);
            endMesh.Faces.AddFace(0, sides, 1); // last face wraps*/
        }
    }

}
