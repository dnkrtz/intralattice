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
// ConvexHull -> Implements a 3D convex hull algorithm that makes certain assumptions
// NormaliseMesh -> Adjusts orientation of face normals 
// SleeveStitch -> Constructs the sleeve mesh faces (stitches the vertices)
// EndFaceStitch -> Constructs the endface mesh faces (needed for single strut nodes)

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.MeshModule
{
    public class MeshTools
    {

        public static Mesh Hull(List<Point3d> Pts)
        {
            Mesh Msh = new Mesh();

            List<int> Indices = new List<int>();
            Line HullStart = new Line(Pts[0], Pts[1]);
            Indices.Add(0); Indices.Add(1);

            bool PlaneSet = false;
            Plane PlaneStart = Plane.Unset;

            for (int P = 2; P < Pts.Count; P++)
            {
                if (!PlaneSet)
                {
                    if (HullStart.ClosestPoint(Pts[P], false).DistanceTo(Pts[P]) > RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                        PlaneSet = true;
                        Indices.Add(P);
                        PlaneStart = new Plane(Pts[0], Pts[1], Pts[P]);
                    }
                }
                else
                {
                    if (PlaneStart.DistanceTo(Pts[P]) > RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                        Indices.Add(P);
                        break;
                    }
                }
            }

            Indices.Reverse();

            foreach (int I in Indices)
            {
                Msh.Vertices.Add(Pts[I]);
                Pts.RemoveAt(I);
            }

            Msh.Faces.AddFace(new MeshFace(0, 1, 2));
            Msh.Faces.AddFace(new MeshFace(0, 2, 3));
            Msh.Faces.AddFace(new MeshFace(0, 3, 1));
            Msh.Faces.AddFace(new MeshFace(1, 2, 3));

            do
            {
                NormaliseMesh(ref Msh);
                GrowHull(ref Msh, Pts[0]);
                Pts.RemoveAt(0);
            } while (Pts.Count > 0);

            Msh.Vertices.CullUnused();

            return Msh;
        }

        public static void GrowHull(ref Mesh Msh, Point3d Pt)
        {

            double Tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 0.1;//RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
            double AngleTest = (Math.PI * 0.5) - ((0.1 / 360) * (Math.PI * 2.0));

            MeshPoint CP = Msh.ClosestMeshPoint(Pt, 0);

            if (CP.Point.DistanceTo(Pt) < Tol)
            {
                int[] EdgeIndices = Msh.TopologyEdges.GetEdgesForFace(CP.FaceIndex);
                Msh.Faces.RemoveAt(CP.FaceIndex);
                Msh.Vertices.Add(Pt);
                List<MeshFace> AddFaces = new List<MeshFace>();

                for (int EdgeIdx = 0; EdgeIdx < EdgeIndices.Length; EdgeIdx++)
                {
                    Rhino.IndexPair VertexPair = Msh.TopologyEdges.GetTopologyVertices(EdgeIndices[EdgeIdx]);
                    AddFaces.Add(new MeshFace(Msh.TopologyVertices.MeshVertexIndices(VertexPair.I)[0], Msh.TopologyVertices.MeshVertexIndices(VertexPair.J)[0], Msh.Vertices.Count - 1));
                }
                Msh.Faces.AddFaces(AddFaces);
                return;
            }
            else if (Msh.IsPointInside(Pt, Tol, true)) { return; }
            else
            {
                Msh.FaceNormals.ComputeFaceNormals();
                List<int> DeleteFaces = new List<int>();
                for (int FaceIdx = 0; FaceIdx < Msh.Faces.Count; FaceIdx++)
                {
                    Vector3d VecTest = new Vector3d(Msh.Faces.GetFaceCenter(FaceIdx) - Pt);
                    Plane PlaneTest = new Plane(Msh.Faces.GetFaceCenter(FaceIdx), Msh.FaceNormals[FaceIdx]);
                    if (Vector3d.VectorAngle(PlaneTest.ZAxis, VecTest) > AngleTest || Math.Abs(PlaneTest.DistanceTo(Pt)) < Tol) { DeleteFaces.Add(FaceIdx); }
                }
                Msh.Faces.DeleteFaces(DeleteFaces);
                Msh.Vertices.Add(Pt);
                List<MeshFace> AddFaces = new List<MeshFace>();
                for (int EdgeIdx = 0; EdgeIdx < Msh.TopologyEdges.Count; EdgeIdx++)
                {
                    if (!Msh.TopologyEdges.IsSwappableEdge(EdgeIdx))
                    {
                        IndexPair VertexPair = Msh.TopologyEdges.GetTopologyVertices(EdgeIdx);
                        AddFaces.Add(new MeshFace(Msh.TopologyVertices.MeshVertexIndices(VertexPair.I)[0], Msh.TopologyVertices.MeshVertexIndices(VertexPair.J)[0], Msh.Vertices.Count - 1));
                    }
                }
                Msh.Faces.AddFaces(AddFaces);
                return;
            }

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputStruts"></param>
        /// <param name="nodes"></param>
        /// <param name="nodePairs"></param>
        /// <param name="struts"></param>
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

        public static void CreateKnuckle(Plane plane, int sides, double radius, double startAngle, out List<Point3d> Vtc)
        {
            Vtc = new List<Point3d>();

            // this loop rotates around the strut, creating vertices
            for (int k = 0; k < sides; k++)
            {
                double angle = k * 2 * Math.PI / sides + startAngle;
                Vtc.Add(plane.PointAt(radius * Math.Cos(angle), radius * Math.Sin(angle))); // create vertex
            }
        }

        public static List<Point3f> Point3dToPoint3f(List<Point3d> in3d)
        {
            var out3f = new List<Point3f>();

            foreach (Point3d pt3d in in3d)
                out3f.Add(new Point3f((float)pt3d.X, (float)pt3d.Y, (float)pt3d.Z));

            return out3f;
        }

        public static bool FilterOffset(double x, List<double> offsets, double tol, out double offset)
        {
            offset = x;

            foreach (double y in offsets)
            {
                double variance = x > y ? x - y : y - x;
                if (variance < tol)
                {
                    offset = y;
                    return true;
                }
            }

            return false;
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

        
    }

}
