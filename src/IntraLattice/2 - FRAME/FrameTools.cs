using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This is a set of methods used by the frame components
// =====================================================
// TopologyNeighbours -> Computes the tree paths of a node's neighbours, based on the desired lattice topology.

// Written by Aidan Kurtz (http://aidankurtz.com)


namespace IntraLattice
{
    public class FrameTools
    {
        /// <summary>
        /// Defines the unit cell topology as a nodes' relationship with neighbours
        /// The conditional statements simply ensure we are within the bounds of the grid
        /// </summary>
        public static void TopologyNeighbours(ref List<GH_Path> neighbourPaths, int topo, int[] N, int u, int v, int w)
        {
            // BASIC
            if ( topo == 0 )
            {
                if (u<N[0])                                          neighbourPaths.Add(new GH_Path(u+1, v, w));
                if (v<N[1])                                          neighbourPaths.Add(new GH_Path(u, v+1, w));
                if (w<N[2])                                          neighbourPaths.Add(new GH_Path(u, v, w+1));
            }
            // X
            else if ( topo == 1 )
            {
                if ((u<N[0]) && (v<N[1]) && (w<N[2]))               neighbourPaths.Add(new GH_Path(u+1, v+1, w+1));
                if ((u<N[0]) && (v>0) && (w<N[2]))                  neighbourPaths.Add(new GH_Path(u+1, v-1, w+1));
                if ((u > 0) && (v > 0) && (w < N[2]))               neighbourPaths.Add(new GH_Path(u-1, v-1, w+1));
                if ((u > 0) && (v < N[1]) && (w < N[2]))            neighbourPaths.Add(new GH_Path(u-1, v+1, w+1));
            }
            // STAR
            else if ( topo == 2 )
            {
                if ((u < N[0]) && (v < N[1]) && (w < N[2]))         neighbourPaths.Add(new GH_Path(u+1, v+1, w+1));
                if ((u < N[0]) && (v > 0) && (w < N[2]))            neighbourPaths.Add(new GH_Path(u+1, v-1, w+1));
                if ((u > 0) && (v > 0) && (w < N[2]))               neighbourPaths.Add(new GH_Path(u-1, v-1, w+1));
                if ((u > 0) && (v < N[1]) && (w < N[2]))            neighbourPaths.Add(new GH_Path(u-1, v+1, w+1));
                if (u < N[0])                                       neighbourPaths.Add(new GH_Path(u+1, v, w));
                if (v < N[1])                                       neighbourPaths.Add(new GH_Path(u, v+1, w));
            }
            // STAR2
            else if ( topo == 3 )
            {
                if ((u < N[0]) && (v < N[1]) && (w < N[2]))         neighbourPaths.Add(new GH_Path(u+1, v+1, w+1));
                if ((u < N[0]) && (v > 0) && (w < N[2]))            neighbourPaths.Add(new GH_Path(u+1, v-1, w+1));
                if ((u > 0) && (v > 0) && (w < N[2]))               neighbourPaths.Add(new GH_Path(u-1, v-1, w+1));
                if ((u > 0) && (v < N[1]) && (w < N[2]))            neighbourPaths.Add(new GH_Path(u-1, v+1, w+1));
                if (u < N[0])                                       neighbourPaths.Add(new GH_Path(u+1, v, w));
                if (v < N[1])                                       neighbourPaths.Add(new GH_Path(u, v+1, w));
                if (w < N[2])                                       neighbourPaths.Add(new GH_Path(u, v, w+1));
            }
            // OCTAHEDRAL
            else if ( topo == 4 )
            {
                            
            }
        }

        public static GH_Line TrimStrut(Point3d node0, Point3d node1, Point3d intersectionPt, bool[] isInside)
        {
            LineCurve testStrut = new LineCurve(new Line(node0, node1), 0, 1);  // set line, with curve parameter domain [0,1]

            // We only create strut if the trimmed strut is a certain length
            double strutLength = node0.DistanceTo(node1);

            if (isInside[0])
            {
                double testLength = intersectionPt.DistanceTo(node0);
                if (testLength < strutLength * 0.1)         return null;    // do not create strut if trimmed strut is less than 10% of the strut length
                else if (testLength > strutLength * 0.9)    return new GH_Line(new Line(node0, node1)); // create full strut if >90% of strut length
                else                                        return new GH_Line(new Line(node0, intersectionPt));
            }
            if (isInside[1])    
            {
                double testLength = intersectionPt.DistanceTo(node1);
                if (testLength < strutLength * 0.1)         return null;
                else if (testLength > strutLength * 0.9)    return new GH_Line(new Line(node0, node1));
                else                                        return new GH_Line(new Line(node1, intersectionPt));
            }

            // If no intersection was found, something went wrong, don't create a strut, skip to next loop
            return null;
        }
    }
}
