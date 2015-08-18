using Grasshopper;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.Data
{
    public enum LatticeType
    {
        None = 0,
        Uniform = 1,
        ConformUVW = 2,
        MorphUVW = 3,
    }

    public enum LatticeNodeState
    {
        Outside = 0,
        Inside = 1,
        Boundary = 2,
    }

    class Lattice
    {
        #region Fields
        private LatticeType m_type;
        private DataTree<LatticeNode> m_nodes;
        private List<LatticeStrut> m_struts;
        #endregion

        #region Constructors
        public Lattice(LatticeType type)
        {
            m_type = type;
            m_nodes = new DataTree<LatticeNode>();
            m_struts = new List<LatticeStrut>();
        }
        #endregion

        #region Properties
        public LatticeType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        public DataTree<LatticeNode> Nodes
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
        /// <summary>
        /// Maps cell topology to UVWI node map (linear struts).         
        /// </summary>
        public List<Curve> ConformMapping(LatticeCell cell, float[] N)
        {
            var struts = new List<Curve>();

            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair cellStrut in cell.NodePairs)
                        {
                            // prepare the path of the nodes (path in tree)
                            int[] IRel = cell.NodePaths[cellStrut.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[cellStrut.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2], IRel[3]); // absolute path
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2], JRel[3]);

                            // make sure both nodes exist (will be false at boundaries)
                            if (Nodes.PathExists(IPath) && Nodes.PathExists(JPath))
                            {
                                Point3d node1 = Nodes[IPath, 0].Point3d;
                                Point3d node2 = Nodes[JPath, 0].Point3d;

                                LineCurve curve = new LineCurve(node1, node2);
                                if (curve != null && curve.IsValid)
                                {
                                    struts.Add(curve);
                                    Struts.Add(new LatticeStrut(curve));
                                }
                            }
                        }
                    }
                }
            }

            return struts;
        }
        /// <summary>
        /// Morphs cell topology to UVWI node map (morphed struts).
        /// </summary>
        public List<Curve> MorphMapping(LatticeCell cell, DataTree<GeometryBase> spaceTree, float[] N)
        {
            var struts = new List<Curve>();

            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair cellStrut in cell.NodePairs)
                        {
                            // prepare the path of the nodes (path in tree)
                            int[] IRel = cell.NodePaths[cellStrut.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[cellStrut.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2], IRel[3]); // absolute path
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2], JRel[3]);

                            // make sure both nodes exist (will be false at boundaries)
                            if (Nodes.PathExists(IPath) && Nodes.PathExists(JPath))
                            {
                                Point3d node1 = Nodes[IPath, 0].Point3d;
                                Point3d node2 = Nodes[JPath, 0].Point3d;

                                GH_Path spacePath;

                                // If strut is along boundary, we must use the previous morph space (since one does not exist beyond the boundary) 
                                if (u == N[0] && v == N[1])
                                    spacePath = new GH_Path(u - 1, v - 1);
                                else if (u == N[0])
                                    spacePath = new GH_Path(u - 1, v);
                                else if (v == N[1])
                                    spacePath = new GH_Path(u, v - 1);
                                else
                                    spacePath = new GH_Path(u, v);

                                GeometryBase ss1 = spaceTree[spacePath, 0]; // retrieve uv cell space (will be casted in the tempPt loop)
                                GeometryBase ss2 = spaceTree[spacePath, 1];

                                // Discretize the unit cell line for morph mapping
                                int ptCount = 16;
                                //int divNumber = (int)(node1.DistanceTo(node2) / morphTol);    // number of discrete segments
                                var templatePts = new List<Point3d>();   // unitized cell points (x,y of these points are u,v of sub-surface)
                                Line templateLine = new Line(cell.Nodes[cellStrut.I], cell.Nodes[cellStrut.J]);
                                for (int ptIndex = 0; ptIndex <= ptCount; ptIndex++)
                                    templatePts.Add(templateLine.PointAt(ptIndex / (double)ptCount));

                                // We will map the lines' points to its uvw cell-space
                                var controlPoints = new List<Point3d>();    // interpolation points in space

                                foreach (Point3d tempPt in templatePts)
                                {
                                    Point3d surfPt;
                                    Vector3d[] surfDerivs;
                                    // uv params are simply the xy coordinate of the template point
                                    double uParam = tempPt.X;
                                    double vParam = tempPt.Y;
                                    // if at boundary, we're using a previous morph space, so reverse the parameter(s)
                                    if (u == N[0]) uParam = 1 - uParam;
                                    if (v == N[1]) vParam = 1 - vParam;

                                    // Now, we will map the template point to the uvw-space
                                    ((Surface)ss1).Evaluate(uParam, vParam, 0, out surfPt, out surfDerivs);
                                    Vector3d wVect = Vector3d.Unset;
                                    switch (ss2.ObjectType)
                                    {
                                        case ObjectType.Point:      // point
                                            wVect = ((Point)ss2).Location - surfPt; ;
                                            break;
                                        case ObjectType.Curve:      // axis
                                            wVect = ((Curve)ss2).PointAt(uParam) - surfPt;
                                            break;
                                        case ObjectType.Surface:    // surface
                                            Point3d surfPt2;
                                            Vector3d[] surfDerivs2;
                                            ((Surface)ss2).Evaluate(uParam, vParam, 0, out surfPt2, out surfDerivs2);
                                            wVect = surfPt2 - surfPt;
                                            break;
                                    }
                                    // The mapped point
                                    Point3d uvwPt = surfPt + wVect * (w + tempPt.Z) / N[2];
                                    controlPoints.Add(uvwPt);

                                }

                                // Now create interpolated curve based on control points
                                Curve curve = Curve.CreateInterpolatedCurve(controlPoints, 3);

                                if (curve != null && curve.IsValid)
                                {
                                    struts.Add(curve);
                                    Struts.Add(new LatticeStrut(curve));
                                }
                            }
                        }
                    }
                }
            }

            return struts;
        }
        /// <summary>
        /// Maps cell topology to the node grid and trims to the design space
        /// =================================================================
        /// =================================================================
        /// </summary>
        public List<Curve> UniformMapping(LatticeCell cell, GeometryBase designSpace, int spaceType, float[] N, double minStrutLength)
        {
            List<Curve> struts = new List<Curve>();
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair cellStrut in cell.NodePairs)
                        {
                            // prepare the path of the nodes (path in tree)
                            int[] IRel = cell.NodePaths[cellStrut.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[cellStrut.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2], IRel[3]);
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2], JRel[3]);

                            // make sure both nodes exist (will be false at boundaries)
                            if (Nodes.PathExists(IPath) && Nodes.PathExists(JPath))
                            {
                                LatticeNode node1 = Nodes[IPath, 0];
                                LatticeNode node2 = Nodes[JPath, 0];

                                Curve fullCurve = new LineCurve(node1.Point3d, node2.Point3d);

                                // If both nodes are inside, add full strut
                                if (node1.IsInside && node2.IsInside)
                                {
                                    Struts.Add(new LatticeStrut(fullCurve));
                                    struts.Add(fullCurve);
                                }
                                // If neither node is inside, skip to next loop
                                else if (!node1.IsInside && !node2.IsInside)
                                {
                                    continue;
                                }
                                // Else, strut requires trimming
                                else
                                {
                                    // We are going to find the intersection point with the design space
                                    Point3d[] intersectionPts = null;
                                    Curve[] overlapCurves = null;
                                    LineCurve strutToTrim = null;

                                    switch (spaceType)
                                    {
                                        // Brep design space
                                        case 1:
                                            strutToTrim = new LineCurve(node1.Point3d, node2.Point3d);
                                            // find intersection point
                                            Intersection.CurveBrep(strutToTrim, (Brep)designSpace, tol, out overlapCurves, out intersectionPts);
                                            break;
                                        // Mesh design space
                                        case 2:
                                            int[] faceIds;  // dummy variable for MeshLine call
                                            strutToTrim = new LineCurve(node1.Point3d, node2.Point3d);
                                            // find intersection point
                                            intersectionPts = Intersection.MeshLine((Mesh)designSpace, strutToTrim.Line, out faceIds);
                                            break;
                                        // Solid surface design space
                                        case 3:
                                            overlapCurves = null;   // dummy variable for CurveBrep call
                                            strutToTrim = new LineCurve(node1.Point3d, node2.Point3d);
                                            // find intersection point
                                            Intersection.CurveBrep(strutToTrim, ((Surface)designSpace).ToBrep(), tol, out overlapCurves, out intersectionPts);
                                            break;
                                    }

                                    LineCurve testLine = null;
                                    // Now, if an intersection point was found, trim the strut
                                    if (intersectionPts.Length > 0)
                                    {
                                        testLine = TrimStrut(node1, node2, intersectionPts[0], minStrutLength);
                                        // if the strut was succesfully trimmed, add it to the list
                                        if (testLine != null)
                                        {
                                            struts.Add(testLine);
                                            Struts.Add(new LatticeStrut(testLine));
                                        }
                                    }
                                    else if (overlapCurves != null && overlapCurves.Length > 0)
                                    {
                                        struts.Add(overlapCurves[0]);
                                        Struts.Add(new LatticeStrut(overlapCurves[0]));
                                    }

                                }
                            }
                        }
                    }
                }
            }

            // Remove the external nodes
            List<LatticeNode> nodes = Nodes.AllData();
            for (int i = 0; i < nodes.Count; i++ )
            {
                if (nodes[i].IsInside)
                    continue;
                else
                    ;
                    // REMOVE PATH
                    // TO BE ADDED, ONCE GOO WRAPPER OF LATTICENODE IS DONE.
            }

            return struts;

        }
        /// <summary>
        /// Trims strut with known intersection point, returning  the trimmed LineCurve which is inside the space
        /// =================================================================
        /// - Intersection point and information about inside/outside state are passed to this method, to know where to trim and which side to keep.
        /// =================================================================
        /// </summary>
        public LineCurve TrimStrut(LatticeNode node1, LatticeNode node2, Point3d intersectionPt, double minStrutLength)
        {
            LineCurve testStrut = new LineCurve(new Line(node1.Point3d, node2.Point3d), 0, 1);  // set line, with curve parameter domain [0,1]

            if (node1.IsInside)
            {
                double trimmedLength = intersectionPt.DistanceTo(node1.Point3d);
                if (trimmedLength > minStrutLength)
                {
                    Nodes.Add(new LatticeNode(intersectionPt, LatticeNodeState.Boundary));
                    return new LineCurve(node1.Point3d, intersectionPt);
                }
                else
                    node1.State = LatticeNodeState.Boundary;
            }
            
            if (node2.IsInside)
            {
                double trimmedLength = intersectionPt.DistanceTo(node2.Point3d);
                if (trimmedLength > minStrutLength)
                {
                    Nodes.Add(new LatticeNode(intersectionPt, LatticeNodeState.Boundary));
                    return new LineCurve(node2.Point3d, intersectionPt);
                }
                else
                    node2.State = LatticeNodeState.Boundary;
            }

            return null;
        }

        #endregion
    }

    class LatticeNode
    {
        #region Fields
        private Point3d m_point3d;
        private LatticeNodeState m_state;
        private List<int> m_strutIndices;
        #endregion

        #region Constructors
        public LatticeNode()
        {
            m_point3d = Point3d.Unset;
            m_state = LatticeNodeState.Inside;
            m_strutIndices = new List<int>();
        }
        public LatticeNode(Point3d point3d)
        {
            m_point3d = point3d;
            m_state = LatticeNodeState.Inside;
            m_strutIndices = new List<int>();
        }
        public LatticeNode(Point3d point3d, LatticeNodeState state)
        {
            m_point3d = point3d;
            m_state = state;
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
        public LatticeNodeState State
        {
            get { return m_state; }
            set { m_state = value; }
        }
        /// <summary>
        /// Indices of the struts associated with this node.
        /// </summary>
        public List<int> StrutIndices
        {
            get { return m_strutIndices; }
            set { m_strutIndices = value; }
        }
        public bool IsInside
        {
            get
            {
                if (m_state == LatticeNodeState.Outside)
                    return false;
                else
                    return true;
            }
        }
        #endregion

        #region Methods
        // none yet
        #endregion
    }

    class LatticeStrut
    {
        #region Fields
        private Curve m_curve;
        private PathPair m_nodePair;
        #endregion

        #region Constructors
        public LatticeStrut()
        {
            m_curve = null;
            m_nodePair = new IndexPair();
        }
        public LatticeStrut(Curve curve)
        {
            m_curve = curve;
            m_nodePair = new IndexPair();
        }
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
