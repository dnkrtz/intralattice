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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

// Summary:     This set of classes is used to generate a lattice wireframe.
//              Refer to the developer documentation for more information.
// =====================================================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Data
{
    /// <summary>
    /// Represents the lattice as a set of nodes in a UVW tree.
    /// Once the nodes are set, the various mapping methods can be used to 
    /// map the unit cell topology to the node tree.
    /// </summary>
    public class Lattice
    {
        #region Fields
        private DataTree<LatticeNode> m_nodes;
        private List<Curve> m_struts;
        #endregion

        #region Constructors
        public Lattice()
        {
            m_nodes = new DataTree<LatticeNode>();
            m_struts = new List<Curve>();
        }
        public Lattice Duplicate()
        {
            using (MemoryStream stream = new MemoryStream())
            {

                if (this.GetType().IsSerializable)
                {

                    BinaryFormatter formatter = new BinaryFormatter();

                    formatter.Serialize(stream, this);

                    stream.Position = 0;

                    return (Lattice)formatter.Deserialize(stream);

                }
                return null;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Nodes as a UVWi tree.
        /// </summary>
        public DataTree<LatticeNode> Nodes
        {
            get { return m_nodes; }
            set { m_nodes = value; }
        }
        /// <summary>
        /// Struts as a list of curves, to be output.
        /// </summary>
        public List<Curve> Struts
        {
            get { return m_struts; }
            set { m_struts = value; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Maps cell topology to UVWI node map (linear struts).         
        /// </summary>
        public void ConformMapping(UnitCell cell, float[] N)
        {
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair nodePair in cell.NodePairs)
                        {
                            // prepare the path of the nodes (path in tree)
                            int[] IRel = cell.NodePaths[nodePair.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[nodePair.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2]); // absolute path
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2]);

                            // make sure the cell exists
                            // no cells exist beyond the boundary + 1
                            if (Nodes.PathExists(IPath) && Nodes.PathExists(JPath))
                            {
                                LatticeNode node1 = Nodes[IPath, IRel[3]];
                                LatticeNode node2 = Nodes[JPath, JRel[3]];
                                // make sure both nodes exist:
                                // null nodes either belong to other cells, or are beyond the upper uvw boundary
                                if (node1 != null && node2 != null)
                                {
                                    LineCurve curve = new LineCurve(node1.Point3d, node2.Point3d);
                                    if (curve != null && curve.IsValid)
                                    {
                                        Struts.Add(curve);
                                    }
                                }
                            }
                            
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Morphs cell topology to UVWI node map (morphed struts).
        /// </summary>
        public void MorphMapping(UnitCell cell, DataTree<GeometryBase> spaceTree, float[] N)
        {
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair nodePair in cell.NodePairs)
                        {
                            // prepare the path of the nodes (path in tree)
                            int[] IRel = cell.NodePaths[nodePair.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[nodePair.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2]); // absolute path
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2]);

                            // make sure the cell exists
                            // no cells exist beyond the boundary + 1
                            if (Nodes.PathExists(IPath) && Nodes.PathExists(JPath))
                            {
                                LatticeNode node1 = Nodes[IPath, IRel[3]];
                                LatticeNode node2 = Nodes[JPath, JRel[3]];
                                // make sure both nodes exist:
                                // null nodes either belong to other cells, or are beyond the upper uvw boundary
                                if (node1 != null && node2 != null)
                                {
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
                                    Line templateLine = new Line(cell.Nodes[nodePair.I], cell.Nodes[nodePair.J]);
                                    for (int ptIndex = 0; ptIndex <= ptCount; ptIndex++)
                                        templatePts.Add(templateLine.PointAt(ptIndex / (double)ptCount));

                                    // We will map the lines' points to its uvw cell-space
                                    var controlPoints = new List<Point3d>();    // interpolation points in space

                                    foreach (Point3d tempPt in templatePts)
                                    {
                                        Point3d surfPt;
                                        Vector3d[] surfDerivs;
                                        // uv params on unitized sub-surface are simply the xy coordinate of the template point
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
                                        Struts.Add(curve);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Maps cell topology to the node grid and trims to the design space
        /// =================================================================
        /// =================================================================
        /// </summary>
        public void UniformMapping(UnitCell cell, GeometryBase designSpace, int spaceType, float[] N, double minStrutLength)
        {
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair nodePair in cell.NodePairs)
                        {
                            // prepare the path of the nodes (path in tree)
                            int[] IRel = cell.NodePaths[nodePair.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[nodePair.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2]);
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2]);

                            // make sure the cell exists
                            if (Nodes.PathExists(IPath) && Nodes.PathExists(JPath))
                            {
                                LatticeNode node1 = Nodes[IPath, IRel[3]];
                                LatticeNode node2 = Nodes[JPath, JRel[3]];
                                // make sure both nodes exist:
                                // null nodes either belong to other cells, or are beyond the upper uvw boundary
                                if (node1 != null && node2 != null)
                                {
                                    Curve fullCurve = new LineCurve(node1.Point3d, node2.Point3d);

                                    // If both nodes are inside, add full strut
                                    if (node1.IsInside && node2.IsInside)
                                    {
                                        Struts.Add(fullCurve);
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
                                            testLine = AddTrimmedStrut(node1, node2, intersectionPts[0], minStrutLength);
                                            // if the strut was succesfully trimmed, add it to the list
                                            if (testLine != null)
                                            {
                                                Struts.Add(testLine);
                                            }
                                        }
                                        else if (overlapCurves != null && overlapCurves.Length > 0)
                                        {
                                            Struts.Add(overlapCurves[0]);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Trims strut with known intersection point, returning  the trimmed LineCurve which is inside the space.
        /// </summary>
        public LineCurve AddTrimmedStrut(LatticeNode node1, LatticeNode node2, Point3d intersectionPt, double minStrutLength)
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

    /// <summary>
    /// Represents a lattice node. Could be extended to include more information. This will do for now.
    /// </summary>
    [Serializable]
    public class LatticeNode
    {
        #region Fields
        private Point3d m_point3d;
        private LatticeNodeState m_state;
        #endregion

        #region Constructors
        public LatticeNode()
        {
            m_point3d = Point3d.Unset;
            m_state = LatticeNodeState.Inside;
        }
        public LatticeNode(Point3d point3d)
        {
            m_point3d = point3d;
            m_state = LatticeNodeState.Inside;
        }
        public LatticeNode(Point3d point3d, LatticeNodeState state)
        {
            m_point3d = point3d;
            m_state = state;
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

    /// <summary>
    /// Represents the state of the node, with respect to the design space.
    /// </summary>
    public enum LatticeNodeState
    {
        Outside = 0,
        Inside = 1,
        Boundary = 2,
    }

}
