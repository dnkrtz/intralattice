using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.Data
{
    public class Node
    {
        /// <summary>
        /// Public constructor
        /// </summary>
        /// <param name="SetPoint3d">Location of node as Point3d.</param>
        public Node(Point3d SetPoint3d)
        {
            this.Point3d = SetPoint3d;
            this.StrutIndices = new List<int>();
            this.PlateIndices = new List<int>();
        }

        // Properties
        /// <summary>
        /// Coordinates of node.
        /// </summary>
        public Point3d Point3d { get; set; }
        /// <summary>
        /// Strut radius at node.
        /// </summary>
        public double Radius { get; set; }
        /// <summary>
        /// Indices of the struts associated with this node.
        /// </summary>
        public List<int> StrutIndices { get; set; }
        /// <summary>
        /// Indices of the plates associated with this node (parallel to StrutIndices)
        /// </summary>
        public List<int> PlateIndices { get; set; }
    }
}
