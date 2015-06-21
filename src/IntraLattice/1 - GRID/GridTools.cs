using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;

namespace IntraLattice
{
    public class GridTools
    {
        /// <summary>
        /// Generates 3D grid of points as data tree
        /// BasePlane - Defines orientation of lattice (origin is important, it is the lattice basepoint)
        /// CS - Cell size in each direction
        /// N - is the number of unit cells in each direction.
        /// BDS - Optional design space parameter
        /// </summary>
        public static void MakeGridBox(ref GH_Structure<GH_Point> gridTree, Plane basePlane, List<double> CS, List<int> N, Brep brepDS = null)
        {
            // Define iteration vectors in each direction (accounting for Cell Size)
            Vector3d vectorX = CS[0] * basePlane.XAxis;
            Vector3d vectorY = CS[1] * basePlane.YAxis;
            Vector3d vectorZ = CS[2] * basePlane.ZAxis;

            Point3d currentPt = new Point3d();

            // Create grid of points (as data tree)
            for (int i = 0; i <= N[0]; i++)
            {
                for (int j = 0; j <= N[1]; j++)
                {
                    for (int k = 0; k <= N[2]; k++)
                    {
                        // Compute position vector
                        Vector3d V = i * vectorX + j * vectorY + k * vectorZ;
                        currentPt = basePlane.Origin + V;
                        
                        GH_Path TreePath = new GH_Path(i, j, k);            // Construct path in tree
                        gridTree.Append(new GH_Point(currentPt), TreePath);     // Add point to tree

                    }
                }
            }
        }

    }
}