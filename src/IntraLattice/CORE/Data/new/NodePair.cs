using Grasshopper.Kernel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.Data
{
    public class NodePair
    {
        #region Fields
        private LatticeNode m_i;
        private LatticeNode m_j;
        #endregion

        #region Constructors
        public NodePair()
        {
            m_i = null;
            m_j = null;
        }
        public NodePair(LatticeNode i, LatticeNode j)
        {
            m_i = i;
            m_j = j;
        }
        #endregion

        #region Properties
        public LatticeNode I
        {
            get { return m_i; }
            set { m_i = value; }
        }
        public LatticeNode J
        {
            get { return m_j; }
            set { m_j = value; }
        }
        #endregion
    }
}
