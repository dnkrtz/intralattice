using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
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
    }
}
