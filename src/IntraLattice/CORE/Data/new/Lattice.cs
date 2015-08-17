using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.Data
{
    public enum LatticeType
    {
        Uniform = 0,
        ConformUVW = 1,
        MorphUVW = 2,
    }

    class Lattice
    {
        #region Fields
        private LatticeType m_type;
        private GH_Structure<LatticeNode> m_nodes;
        private List<LatticeStrut> m_struts;
        #endregion

        #region Constructors

        #endregion

        #region Properties
        public LatticeType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        public GH_Structure<LatticeNode> Nodes
        {
            get { return m_nodes; }
            set { m_nodes = value; }
        }
        public List<LatticeStrut> Struts
        {
            get { return m_struts; }
            set { m_struts = value; }
        }
        #endregion

        #region Methods
        // none yet
        #endregion
    }

    class LatticeNode
    {
        #region Fields
        private Point3d m_point3d;
        private List<int> m_strutIndices;
        #endregion

        #region Constructors
        public LatticeNode()
        {
            m_point3d = Point3d.Unset;
            m_strutIndices = new List<int>();
        }
        public LatticeNode(Point3d point3d)
        {
            m_point3d = point3d;
            m_strutIndices = new List<int>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Coordinates of node.
        /// </summary>
        public Point3d Point3d
        {
            get { return m_point3d; }
            set { m_point3d = value; }
        }
        /// <summary>
        /// Indices of the struts associated with this node.
        /// </summary>
        public List<int> StrutIndices
        {
            get { return m_strutIndices; }
            set { m_strutIndices = value; }
        }
        #endregion

        #region Methods
        // none for now
        #endregion
    }

    class LatticeStrut
    {
        #region Fields
        private Curve m_curve;
        private IndexPair m_nodePair;
        #endregion

        #region Constructors
        public LatticeStrut(Curve curve, IndexPair nodePair)
        {
            m_curve = curve;
            m_nodePair = nodePair;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The strut's curve. (may be linear)
        /// </summary>
        public Curve Curve
        {
            get { return m_curve; }
            set { m_curve = value; }
        }
        /// <summary>
        /// The pair of node indices of the strut.
        /// </summary>
        public IndexPair NodePair
        {
            get { return m_nodePair; }
            set { m_nodePair = value; }
        }
        #endregion

        #region Methods
        // none yet
        #endregion
    }

}
