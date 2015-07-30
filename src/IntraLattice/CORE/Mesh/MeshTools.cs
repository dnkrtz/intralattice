using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This is a set of methods & objects used by the mesh components
// =====================================================
// ConvexHull -> Implements a 3D convex hull algorithm that assumes all points lie on the hull (which is our case)
// NormalizeMesh -> Adjusts orientation of face normals 
// SleeveStitch -> Constructs the sleeve mesh faces (stitches the vertices)
// EndFaceStitch -> Constructs the endface mesh faces (needed for single strut nodes)
// LatticePlate -> *Object* representing the shared vertices between convex hulls and sleeves, based around a node
// LatticeNode -> *Object* representing a lattice node

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class MeshTools
    {

        /// <summary>
        /// Incremental 3D convex hull algorithm
        /// </summary>
        public static void ConvexHull(ref Mesh hullMesh, List<Point3d> pts, int sides)
        {
            int totalPts = pts.Count;

            // 1. Create initial tetrahedron.
            // Form triangle from 3 first points (lie on same plate, thus, same plane)
            hullMesh.Vertices.Add(pts[0]);
            hullMesh.Vertices.Add(pts[1]);
            hullMesh.Vertices.Add(pts[2]);
            Plane PlaneStart = new Plane(pts[0], pts[1], pts[2]);
            // Form tetrahedron with a 4th point which does not lie on the same plane
            // Point S+1 is the centerpoint of another plate, therefore it is surely on a different plane.
            hullMesh.Vertices.Add(pts[sides + 1]);
            // Stitch faces of tetrahedron
            hullMesh.Faces.AddFace(0, 2, 1);
            hullMesh.Faces.AddFace(0, 3, 2);
            hullMesh.Faces.AddFace(0, 1, 3);
            hullMesh.Faces.AddFace(1, 2, 3);

            // 2. Begin the incremental hulling process
            // Remove points already checked
            pts.RemoveAt(sides + 1);
            pts.RemoveRange(0, 3);
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 0.1;


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
                    if (angle < Math.PI * 0.5 || Math.Abs(planeTest.DistanceTo(pts[i])) < tol) { seenFaces.Add(faceIndex); }
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

            // 3. Remove plate faces
            List<int> deleteFaces = new List<int>();


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
