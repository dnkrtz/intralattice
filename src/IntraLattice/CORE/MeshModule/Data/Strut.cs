using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.MeshModule.Data
{
    public class Strut
    {
        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="SetCurve">The strut curve. (may be linear)</param>
        /// <param name="SetNodePair">The pair of node indices of this strut.</param>
        public Strut(Curve SetCurve, IndexPair SetNodePair)
        {
            this.Curve = SetCurve;
            this.NodePair = SetNodePair;
        }

        // Properties
        /// <summary>
        /// The strut's curve. (may be linear)
        /// </summary>
        public Curve Curve { get; set; }
        /// <summary>
        /// The pair of node indices of the strut.
        /// </summary>
        public IndexPair NodePair { get; set; }
        /// <summary>
        /// The pair of plate indices of the strut.
        /// </summary>
        public IndexPair PlatePair { get; set; }
    }
}
