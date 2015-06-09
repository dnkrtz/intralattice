using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatticeMesh
{
    public class MeshTools
    {
        // CREATES STRUT MESH FACES
        public static void SleeveStitch(ref Mesh StrutMesh, double D, int S)
        {
            // Make stocking mesh
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

        // CREATES ENDFACE MESH FACES
        public static void EndFaceStitch(ref Mesh EndMesh, int S)
        {
            // Make endface mesh
            for (int i = 1; i < S; i++)
            {
                EndMesh.Faces.AddFace(0, i, i + 1);
            }
            EndMesh.Faces.AddFace(0, S, 1); // last face wraps*/
        }
    }

    // The LatticePlate object
    public class LatticePlate
    {
        public int NodeIndex;
        public double Offset;
        public Plane Plane;
        public Vector3d Normal;     // oriented for offset
        public List<Point3d> Vtc = new List<Point3d>();    // The first item in this list is the center point of the plate
        public double Radius;

        /*public LatticePlate(LineCurve SetStrut)
        {
            Strut = SetStrut;
            Strut.PerpendicularFrameAt(0.0, out HullPlane);
            Normal = HullPlane.ZAxis;
        }*/
    }

    public class LatticeNode
    {
        public Point3d Point3d;
        public List<int> PlateIndices = new List<int>();    // Ordered
        
        // constructor
        public LatticeNode(Point3d SetPoint3d)
        {
            Point3d = SetPoint3d;
        }
    }

    
 
}
