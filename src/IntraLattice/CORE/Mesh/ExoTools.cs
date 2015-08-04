using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace IntraLattice.CORE.Mesh
{
    public class ExoTools
    {

        public static void StockingStitch(ref Mesh StrutMesh, int Segments, int S)
        {
            for (int V = 0; V < Segments; V++)
            {
                int Adder = V * S;
                int V1, V2, V3;
                if (V % 2 == 0)
                {
                    V1 = Adder;
                    V2 = Adder + S;
                    V3 = Adder + S + 1;
                    for (int VIdx = 0; VIdx < S; VIdx++)
                    {
                        StrutMesh.Faces.AddFace(V1, V2, V3);
                        V3 -= S;
                        V2 += 1;
                        if (VIdx == S - 1) V2 = Adder + S;
                        StrutMesh.Faces.AddFace(V3, V2, V1);
                        V3 += S + 1;
                        V1 += 1;
                        if (VIdx == S - 2) V3 = Adder + S;
                    }
                }
                else
                {
                    V1 = Adder;
                    V2 = Adder + (S * 2) - 1;
                    for (int VIdx = 0; VIdx < S; VIdx++)
                    {
                        V3 = V1 + S;
                        StrutMesh.Faces.AddFace(V1, V2, V3);
                        if (VIdx == 0) V2 = Adder + S;
                        else V2 += 1;
                        if (VIdx == S - 1) V3 = Adder;
                        else V3 = V1 + 1;
                        StrutMesh.Faces.AddFace(V3, V2, V1);
                        V1 += 1;
                    }
                }
            }
            return;
        }

        public static void VertexAdd(ref Mesh Msh, Plane Pln, double S, double R, bool Affix, Rhino.Collections.Point3dList Pts, Mesh AffixMesh)
        {
            for (int I = 1; I <= S; I++)
            {
                double Angle = ((double)I / S) * 2.0 * Math.PI;
                if (Affix) Msh.Vertices.Add(AffixMesh.Vertices[Pts.ClosestIndex(Pln.PointAt(Math.Cos(Angle) * R, Math.Sin(Angle) * R, 0))]);
                else Msh.Vertices.Add(Pln.PointAt(Math.Cos(Angle) * R, Math.Sin(Angle) * R, 0));
            }
            return;
        }

        public static void Hull(List<Point3d> Pts, ref Mesh Msh)
        {

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


        public static void NormaliseMesh(ref Mesh Msh)
        {
            Msh.Vertices.CombineIdentical(true, true);
            Msh.FaceNormals.ComputeFaceNormals();
            Msh.UnifyNormals();
            Msh.Normals.ComputeNormals();
        }

        public static void OffsetCalculator(double Theta, double R1, double R2, ref double Offset1, ref double Offset2)
        {
            double Perp = Math.PI * 0.5;

            double rA = Math.Min(R1, R2);
            double rB = Math.Max(R1, R2);

            double MaxAngle = Math.Asin(rA / rB) + Perp;

            double OffsetA, OffsetB;

            if (Theta <= Perp)
            {
                OffsetA = Math.Sqrt(Math.Pow((rA / Math.Sin(Theta)), 2) - Math.Pow(rA, 2)) + rB / Math.Sin(Theta);
                OffsetB = Math.Sqrt(Math.Pow((rB / Math.Sin(Theta)), 2) - Math.Pow(rB, 2)) + rA / Math.Sin(Theta);
            }
            else if (rA == rB && Math.Abs(Theta - Math.PI) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
            {
                OffsetA = rA * 0.5;
                OffsetB = rA * 0.5;
            }
            else
            {
                Theta = Math.Min(MaxAngle, Theta);
                OffsetA = (rB * Math.Cos(Theta - Perp)) - ((rA - rB * Math.Sin(Theta - Perp)) / Math.Cos(Theta - Perp)) * Math.Sin(Theta - Perp);
                OffsetB = (rA - rB * Math.Sin(Theta - Perp)) / Math.Cos(Theta - Perp);
            }

            if (R1 <= R2)
            {
                Offset1 = OffsetA;
                Offset2 = OffsetB;
            }
            else
            {
                Offset1 = OffsetB;
                Offset2 = OffsetA;
            }

        }

    }

}