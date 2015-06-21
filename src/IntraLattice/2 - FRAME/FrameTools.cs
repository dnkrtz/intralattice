using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice
{
    public class FrameTools
    {
        /// <summary>
        /// Defines the unit cell topology as a nodes' relationship with neighbours
        /// </summary>
        public static void TopologyNeighbours(ref List<GH_Path> neighbourPaths, int topo, List<int> indx, int i, int j, int k)
        {
            // GRID
            if ( topo == 0 )
            {
                if (i<indx[0])                                          neighbourPaths.Add(new GH_Path(i+1, j, k));
                if (j<indx[1])                                          neighbourPaths.Add(new GH_Path(i, j+1, k));
                if (k<indx[2])                                          neighbourPaths.Add(new GH_Path(i, j, k+1));
            }
            // X
            else if ( topo == 1 )
            {
                if ((i<indx[0]) && (j<indx[1]) && (k<indx[2]))          neighbourPaths.Add(new GH_Path(i+1, j+1, k+1));
                if ((i<indx[0]) && (j>0) && (k<indx[2]))                neighbourPaths.Add(new GH_Path(i+1, j-1, k+1));
                if ((i > 0) && (j > 0) && (k < indx[2]))                neighbourPaths.Add(new GH_Path(i-1, j-1, k+1));
                if ((i > 0) && (j < indx[1]) && (k < indx[2]))          neighbourPaths.Add(new GH_Path(i-1, j+1, k+1));
            }
            // STAR
            else if ( topo == 2 )
            {
                if ((i < indx[0]) && (j < indx[1]) && (k < indx[2]))    neighbourPaths.Add(new GH_Path(i+1, j+1, k+1));
                if ((i < indx[0]) && (j > 0) && (k < indx[2]))          neighbourPaths.Add(new GH_Path(i+1, j-1, k+1));
                if ((i > 0) && (j > 0) && (k < indx[2]))                neighbourPaths.Add(new GH_Path(i-1, j-1, k+1));
                if ((i > 0) && (j < indx[1]) && (k < indx[2]))          neighbourPaths.Add(new GH_Path(i-1, j+1, k+1));
                if (i < indx[0])                                        neighbourPaths.Add(new GH_Path(i+1, j, k));
                if (j < indx[1])                                        neighbourPaths.Add(new GH_Path(i, j+1, k));
            }
            // STAR2
            else if ( topo == 3 )
            {
                if ((i < indx[0]) && (j < indx[1]) && (k < indx[2]))    neighbourPaths.Add(new GH_Path(i+1, j+1, k+1));
                if ((i < indx[0]) && (j > 0) && (k < indx[2]))          neighbourPaths.Add(new GH_Path(i+1, j-1, k+1));
                if ((i > 0) && (j > 0) && (k < indx[2]))                neighbourPaths.Add(new GH_Path(i-1, j-1, k+1));
                if ((i > 0) && (j < indx[1]) && (k < indx[2]))          neighbourPaths.Add(new GH_Path(i-1, j+1, k+1));
                if (i < indx[0])                                        neighbourPaths.Add(new GH_Path(i+1, j, k));
                if (j < indx[1])                                        neighbourPaths.Add(new GH_Path(i, j+1, k));
                if (k < indx[2])                                        neighbourPaths.Add(new GH_Path(i, j, k+1));
            }
            // OCTAHEDRAL
            else if ( topo == 4 )
            {
                            
            }


        }

        internal static void Topology(ref List<GH_Path> NeighbourPaths, int Topo, int[] indx, int i, int j, int k)
        {
            throw new NotImplementedException();
        }
    }


}
