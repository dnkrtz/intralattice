using Rhino;
using Rhino.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.FrameModule.Data
{
    // The UnitCell object
    public class UnitCell
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public UnitCell()
        {
            this.Nodes = new Point3dList();
            this.NodePaths = new List<int[]>();
            this.NodeNeighbours = new List<List<int>>();
            this.NodePairs = new List<IndexPair>();
        }

        /// <summary>
        /// List of unique nodes
        /// </summary>
        public Point3dList Nodes { get; set; }
        /// <summary>
        /// List of relative paths in tree (parallel to Nodes list)
        /// </summary>
        public List<int[]> NodePaths { get; set; }
        /// <summary>
        /// List of node adjacency lists (parallel to Nodes list)
        /// </summary>
        public List<List<int>> NodeNeighbours { get; set; }
        /// <summary>
        /// List of struts as node index pairs
        /// </summary>
        public List<IndexPair> NodePairs { get; set; }
    }
}
