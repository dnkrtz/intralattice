using Grasshopper.Kernel.Data;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.Data
{

    class ExoMesh
    {
        #region Fields
        private List<ExoHull> m_hulls;
        private List<ExoSleeve> m_sleeves;
        private List<ExoPlate> m_plates;
        #endregion

        #region Constructors
        public ExoMesh()
        {
            m_hulls = new List<ExoHull>();
            m_sleeves = new List<ExoSleeve>();
            m_plates = new List<ExoPlate>();
        }
        #endregion

        #region Properties
        public List<ExoHull> Hulls
        {
            get { return m_hulls; }
            set { m_hulls = value; }
        }
        public List<ExoSleeve> Sleeves
        {
            get { return m_sleeves; }
            set { m_sleeves = value; }
        }
        public List<ExoPlate> Plates
        {
            get { return m_plates; }
            set { m_plates = value; }
        }
        #endregion

        #region Methods
        //none yet
        #endregion
    }

    class ExoHull : LatticeNode
    {
        #region Fields
        private List<int> m_plateIndices;
        // Other fields are inherited from LatticeNode
        #endregion

        #region Constructors
        public ExoHull()
        {
            
        }
        #endregion

        #region Properties
        /// <summary>
        /// Indices of the plates associated with this node (parallel to StrutIndices)
        /// </summary>
        public List<int> PlateIndices
        {
            get { return m_plateIndices; }
            set { m_plateIndices = value; }
        }
        #endregion

        #region Methods
        // none yet
        #endregion
    }

    class ExoSleeve : LatticeStrut
    {
        #region Fields
        private IndexPair m_platePair;
        private double m_startRadius;
        private double m_endRadius;
        // Other fields are inherited from LatticeStrut
        #endregion

        #region Constructors
        public ExoSleeve()
        {
            
        }
        #endregion

        #region Properties
        /// <summary>
        /// The pair of plate indices for this sleeve.
        /// </summary>
        public IndexPair PlatePair
        {
            get { return m_platePair; }
            set { m_platePair = value; }
        }
        /// <summary>
        /// The start radius of the sleeve.
        /// </summary>
        public double StartRadius
        {
            get { return m_startRadius; }
            set { m_startRadius = value; }
        }
        /// <summary>
        /// The end radius of the sleeve.
        /// </summary>
        public double EndRadius
        {
            get { return m_endRadius; }
            set { m_endRadius = value; }
        }
        /// <summary>
        /// The average radius of the sleeve.
        /// </summary>
        public double AvgRadius
        {
            get { return (StartRadius + EndRadius) / 2; }
        }
        #endregion

        #region Methods
        // none yet
        #endregion
    }

    class ExoPlate
    {

    }
}
