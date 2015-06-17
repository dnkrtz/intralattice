using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatticeMesh
{
    public class MeshTools
    {

        public static void ConvexHull(ref Mesh HullMesh, List<Point3d> Pts, int S)
        {
            HullMesh.Vertices.Add(Pts[0]);
            HullMesh.Vertices.Add(Pts[1]);
            HullMesh.Vertices.Add(Pts[2]);
            Plane PlaneStart = new Plane(Pts[0], Pts[1], Pts[2]);

            for (int i = S + 1; i < Pts.Count; i++ )
            {
                if ( Math.Abs(PlaneStart.DistanceTo(Pts[i])) > 0.1)
                {
                    HullMesh.Vertices.Add(Pts[i]);
                    break;
                }
            }

            HullMesh.Faces.AddFace(0, 2, 1);
            HullMesh.Faces.AddFace(0, 3, 2);
            HullMesh.Faces.AddFace(0, 1, 3);
            HullMesh.Faces.AddFace(1, 2, 3);            
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

        /// Construts endface mesh (single strut nodes)
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
