using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice
{
    public class MeshTools
    {

        /// <summary>
        /// Incremental 3D convex hull algorithm
        /// This approach is modified to take advantage of certain assumptions about the input
        /// - All points lie on the hull
        /// </summary>
        public static void ConvexHull(ref Mesh HullMesh, List<Point3d> Pts, int S)
        {
            int TotalPts = Pts.Count;

            // 1. Create initial tetrahedron.
            // Form triangle from 3 first points (lie on same plate, thus, same plane)
            HullMesh.Vertices.Add(Pts[0]);
            HullMesh.Vertices.Add(Pts[1]);
            HullMesh.Vertices.Add(Pts[2]);
            Plane PlaneStart = new Plane(Pts[0], Pts[1], Pts[2]);
            // Form tetrahedron with a 4th point which does not lie on the same plane
            // Point S+1 is the centerpoint of another plate, therefore it is surely on a different plane.
            HullMesh.Vertices.Add(Pts[S + 1]);
            // Stitch faces of tetrahedron
            HullMesh.Faces.AddFace(0, 2, 1);
            HullMesh.Faces.AddFace(0, 3, 2);
            HullMesh.Faces.AddFace(0, 1, 3);
            HullMesh.Faces.AddFace(1, 2, 3);

            // 2. Begin the incremental hulling process
            // Remove points already checked
            Pts.RemoveAt(S + 1);
            Pts.RemoveRange(0, 3);
            double Tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 0.1;


            // Loop through the remaining points
            for (int i = 0; i < Pts.Count; i++)
            {
                NormaliseMesh(ref HullMesh);

                // Find visible faces
                List<int> SeenFaces = new List<int>();
                for (int FaceIndex = 0; FaceIndex < HullMesh.Faces.Count; FaceIndex++)
                {
                    Vector3d TestVect = Pts[i] - HullMesh.Faces.GetFaceCenter(FaceIndex);
                    double Angle = Vector3d.VectorAngle(HullMesh.FaceNormals[FaceIndex], TestVect);
                    Plane PlaneTest = new Plane(HullMesh.Faces.GetFaceCenter(FaceIndex), HullMesh.FaceNormals[FaceIndex]);
                    if (Angle < Math.PI * 0.5 || Math.Abs(PlaneTest.DistanceTo(Pts[i])) < Tol) { SeenFaces.Add(FaceIndex); }
                }

                // Remove visible faces
                HullMesh.Faces.DeleteFaces(SeenFaces);
                // Add current point
                HullMesh.Vertices.Add(Pts[i]);

                List<MeshFace> AddFaces = new List<MeshFace>();
                // Close open hull with new vertex
                for (int EdgeIndex = 0; EdgeIndex < HullMesh.TopologyEdges.Count; EdgeIndex++)
                {
                    if (!HullMesh.TopologyEdges.IsSwappableEdge(EdgeIndex))
                    {
                        IndexPair V = HullMesh.TopologyEdges.GetTopologyVertices(EdgeIndex);
                        int I1 = HullMesh.TopologyVertices.MeshVertexIndices(V.I)[0];
                        int I2 = HullMesh.TopologyVertices.MeshVertexIndices(V.J)[0];
                        AddFaces.Add(new MeshFace(I1, I2, HullMesh.Vertices.Count - 1));
                    }
                }
                HullMesh.Faces.AddFaces(AddFaces);
            }

            NormaliseMesh(ref HullMesh);

            // 3. Remove plate faces
            List<int> DeleteFaces = new List<int>();


        }

        /// <summary>
        /// Fix orientation of face normals
        /// </summary>
        public static void NormaliseMesh(ref Mesh Msh)
        {
            if (Msh.SolidOrientation() == -1) Msh.Flip(true, true, true);
            Msh.FaceNormals.ComputeFaceNormals();
            Msh.UnifyNormals();
            Msh.Normals.ComputeNormals();
        }

        /// <summary>
        /// Constructs sleeve mesh faces (stitches the vertices)
        /// </summary>
        public static void SleeveStitch(ref Mesh StrutMesh, double D, int S)
        {
            int V1, V2, V3, V4;
            for (int j = 0; j < D; j++)
            {
                for (int i = 0; i < S; i++)
                {
                    V1 = (j * S) + i;
                    V2 = (j * S) + i + S;
                    V3 = (j * S) + S + (i + 1) % (S);
                    V4 = (j * S) + (i + 1) % (S);

                    StrutMesh.Faces.AddFace(V1, V2, V4);
                    StrutMesh.Faces.AddFace(V2, V3, V4);
                }
            }
        }

        /// <summary>
        /// Construts endface mesh (single strut nodes)
        /// </summary>
        public static void EndFaceStitch(ref Mesh EndMesh, int S)
        {
            // Stitch faces
            for (int i = 1; i < S; i++) EndMesh.Faces.AddFace(0, i, i + 1);
            EndMesh.Faces.AddFace(0, S, 1); // last face wraps*/
        }
    }

    // The LatticePlate object
    public class LatticePlate
    {
        public int NodeIndex;       // index of its parent node
        public double Offset;       // offset value
        public Vector3d Normal;     // direction of offset
        public double Radius;       // radius of the plate
        public List<Point3d> Vtc = new List<Point3d>();    // vertices (at index 0 is the centerpoint vertex)
    }

    public class LatticeNode
    {
        public Point3d Point3d;     // coordinates of node
        public List<int> PlateIndices = new List<int>();    // indices of the plates associated to this node

        // constructor sets coordinate
        public LatticeNode(Point3d SetPoint3d)
        {
            Point3d = SetPoint3d;
        }
    }



}
