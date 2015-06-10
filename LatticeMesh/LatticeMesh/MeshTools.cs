using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LatticeMesh
{
    public class MeshTools
    {
        // Constructs sleeve mesh
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
