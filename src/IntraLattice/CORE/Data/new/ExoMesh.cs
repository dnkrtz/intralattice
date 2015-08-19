using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.Geometry;
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
        /// <summary>
        /// Computes plate offsets required to avoid mesh overlaps.
        /// For linear struts, this is done with simple trig.
        /// For curved struts, sphere intersections are used.
        /// </summary>
        /// <param name="nodeIndex">Index of the node who's plates we are computing offsets for.</param>
        /// <param name="tol">Tolerance for point locations (RhinoDoc.ActiveDoc.ModelAbsoluteTolerance is a good bet).</param>
        /// <returns>True if offsets are valid, false if struts are engulfed by their nodes.</returns>
        public bool ComputeOffsets(int nodeIndex, double tol)
        {
            ExoHull node = Hulls[nodeIndex];
            double minOffset = node.Radius; // the minimum offset is based on the radius at the node

            List<Curve> paths = new List<Curve>();
            List<double> offsets = new List<double>();  // parameter offset (path domains are unitized)

            // Prepare all struts and initialize offsets
            foreach (int strutIndex in node.StrutIndices)
            {
                Curve curve = this.Struts[strutIndex].Curve.DuplicateCurve();
                if (curve.PointAtEnd.EpsilonEquals(node.Point3d, 100 * tol))
                {
                    curve.Reverse(); // reverse direction of curve to start at this node
                    curve.Domain = new Interval(0, 1);
                }

                paths.Add(curve);

                double offsetParam;
                curve.LengthParameter(minOffset, out offsetParam);
                offsets.Add(offsetParam);
            }

            bool convexFound = false;
            bool[] travel;
            int iteration = 0;
            double paramIncrement = offsets[0] / 10;
            // Iterate until a suitable plate layout is found (i.e. ensures the convex hull won't engulf any plate points)
            while (!convexFound && iteration < 500)
            {
                // Prepare list of circles
                List<Circle> circles = new List<Circle>();
                for (int i = 0; i < paths.Count; i++)
                {
                    Plane plane;
                    paths[i].PerpendicularFrameAt(offsets[i], out plane);
                    circles.Add(new Circle(plane, node.Radius));
                }

                // Do stuff here...

                // Loop over all pairs of struts
                travel = new bool[paths.Count];
                for (int a = 0; a < paths.Count; a++)
                {
                    for (int b = a + 1; b < paths.Count; b++)
                    {
                        double p1, p2;
                        var intAB = Intersection.PlaneCircle(circles[a].Plane, circles[b], out p1, out p2);
                        var intBA = Intersection.PlaneCircle(circles[b].Plane, circles[a], out p1, out p2);
                        if (intAB == PlaneCircleIntersection.Secant || intAB == PlaneCircleIntersection.Tangent)
                            travel[a] = true;
                        if (intBA == PlaneCircleIntersection.Secant || intBA == PlaneCircleIntersection.Tangent)
                            travel[b] = true;
                    }
                }

                // Increase offset of plates that intersected, if no intersections, we have a suitable convex layout
                convexFound = true;
                for (int i = 0; i < paths.Count; i++)
                {
                    if (travel[i])
                    {
                        offsets[i] += paramIncrement;
                        convexFound = false;
                    }
                }

                iteration++;
            }

            for (int i = 0; i < paths.Count; i++)
            {
                int plateIndex = node.PlateIndices[i];
                this.Plates[plateIndex].Offset = 1.05 * offsets[i];
            }

            return true;
        }
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
        public ExoSleeve(LatticeStrut baseStrut)
        {
            base.Curve = baseStrut.Curve;   // note: passed by reference
            base.NodePair = baseStrut.NodePair;
            m_platePair = new IndexPair();
            m_startRadius = 0.0;
            m_endRadius = 0.0;
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
