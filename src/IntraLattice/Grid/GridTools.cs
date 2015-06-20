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
        public static void MakeGridBox(ref GH_Structure<GH_Point> GridTree, Plane BasePlane, List<double> CS, List<int> N, Brep BrepDS = null)
        {
            // Define iteration vectors in each direction (accounting for Cell Size)
            Vector3d Vx = CS[0] * BasePlane.XAxis;
            Vector3d Vy = CS[1] * BasePlane.YAxis;
            Vector3d Vz = CS[2] * BasePlane.ZAxis;

            Point3d CurrentPt = new Point3d();

            // Create grid of points (as data tree)
            for (int i = 0; i <= N[0]; i++)
            {
                for (int j = 0; j <= N[1]; j++)
                {
                    for (int k = 0; k <= N[2]; k++)
                    {
                        // Compute position vector
                        Vector3d V = i * Vx + j * Vy + k * Vz;
                        CurrentPt = BasePlane.Origin + V;
                        
                        // For BREP Design Space - checks if point is inside
                        if (BrepDS.IsPointInside(CurrentPt, RhinoMath.SqrtEpsilon, true))
                        {
                            // Neighbours of an inside node must be created (since they share a strut with an inside node, which we will trim)
                            // So before creating the node, we ensure that all its neighbours have been created
                            // This might seem excessive, but it's a robust approach
                            List<GH_Path> Neighbours = new List<GH_Path>();
                            Neighbours.Add(new GH_Path(i-1, j, k));
                            Neighbours.Add(new GH_Path(i, j-1, k));
                            Neighbours.Add(new GH_Path(i, j, k-1));
                            Neighbours.Add(new GH_Path(i+1, j, k));
                            Neighbours.Add(new GH_Path(i, j+1, k));
                            Neighbours.Add(new GH_Path(i, j, k+1));

                            if (!GridTree.PathExists(Neighbours[0])) GridTree.Append(new GH_Point(CurrentPt - Vx), Neighbours[0]);
                            if (!GridTree.PathExists(Neighbours[1])) GridTree.Append(new GH_Point(CurrentPt - Vy), Neighbours[1]);
                            if (!GridTree.PathExists(Neighbours[2])) GridTree.Append(new GH_Point(CurrentPt - Vz), Neighbours[2]);
                            if (!GridTree.PathExists(Neighbours[3])) GridTree.Append(new GH_Point(CurrentPt + Vx), Neighbours[3]);
                            if (!GridTree.PathExists(Neighbours[4])) GridTree.Append(new GH_Point(CurrentPt + Vy), Neighbours[4]);
                            if (!GridTree.PathExists(Neighbours[5])) GridTree.Append(new GH_Point(CurrentPt + Vz), Neighbours[5]);

                            // Now create the current node

                            GH_Path TreePath = new GH_Path(i, j, k);            // Construct path in tree
                            GridTree.Append(new GH_Point(CurrentPt), TreePath);     // Add point to tree
                        }

                    }
                }
            }
        }

    }
}