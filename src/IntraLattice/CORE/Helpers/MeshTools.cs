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
// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Helpers
{
    public class MeshTools
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="sides"></param>
        /// <param name="radius"></param>
        /// <param name="startAngle"></param>
        /// <param name="Vtc"></param>
        public static List<Point3d> CreateKnuckle(Plane plane, int sides, double radius, double startAngle)
        {
            var Vtc = new List<Point3d>();

            // this loop rotates around the strut, creating vertices
            for (int k = 0; k < sides; k++)
            {
                double angle = k * 2 * Math.PI / sides + startAngle;
                Vtc.Add(plane.PointAt(radius * Math.Cos(angle), radius * Math.Sin(angle))); // create vertex
            }

            return Vtc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="in3d"></param>
        /// <returns></returns>
        public static List<Point3f> Point3dToPoint3f(List<Point3d> in3d)
        {
            var out3f = new List<Point3f>();

            foreach (Point3d pt3d in in3d)
                out3f.Add(new Point3f((float)pt3d.X, (float)pt3d.Y, (float)pt3d.Z));

            return out3f;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="offsets"></param>
        /// <param name="tol"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        public static void NormaliseMesh(ref Mesh mesh)
        {
            if (mesh.SolidOrientation() == -1) mesh.Flip(true, true, true);
            mesh.FaceNormals.ComputeFaceNormals();
            mesh.UnifyNormals();
            mesh.Normals.ComputeNormals();
        }
        
    }

}
