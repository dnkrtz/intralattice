using IntraLattice.CORE.Helpers;
using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.Data
{
    // The LatticeCell object represents 
    public class LatticeCell
    {

        #region Fields
        private Point3dList m_nodes;
        private List<IndexPair> m_nodePairs;
        private List<int[]> m_nodePaths;
        #endregion

        #region Constructors
        public LatticeCell()
        {
            m_nodes = new Point3dList();
            m_nodePairs = new List<IndexPair>();
            m_nodePaths = new List<int[]>();
        }
        public LatticeCell(List<Line> rawCell)
        {
            m_nodes = new Point3dList();
            m_nodePairs = new List<IndexPair>();
            m_nodePaths = new List<int[]>();

            ExtractTopology(rawCell);
            NormaliseTopology();
        }
        public LatticeCell Duplicate()
        {
            LatticeCell dup = new LatticeCell();
            foreach (Point3d node in Nodes)
                dup.m_nodes.Add(node);
            foreach (IndexPair nodePair in NodePairs)
                dup.m_nodePairs.Add(nodePair);
            foreach (int[] nodePath in NodePaths)
                dup.m_nodePaths = NodePaths;
            return dup;
        }
        #endregion

        #region Properties
        /// <summary>
        /// List of unique nodes
        /// </summary>
        public Point3dList Nodes
        {
            get { return m_nodes; }
            set { m_nodes = value; }
        }
        /// <summary>
        /// List of struts as node index pairs
        /// </summary>
        public List<IndexPair> NodePairs
        {
            get { return m_nodePairs; }
            set { m_nodePairs = value; }
        }
        /// <summary>
        /// List of relative paths in tree (parallel to Nodes list)
        /// </summary>
        public List<int[]> NodePaths
        {
            get { return m_nodePaths; }
            set { m_nodePaths = value; }
        }
        /// <summary>
        /// Verifies validity of unit cell.
        /// </summary>
        public bool isValid
        {
            get
            {
                int flag = this.CheckValidity();
                if (flag == 1)
                    return true;
                else
                    return false;
            }
        }


        #endregion

        #region Methods
        /// <summary>
        /// Formats the line input into the UnitCell object.
        /// </summary>
        /// <param name="lines"></param>
        private void ExtractTopology(List<Line> lines)
        {
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            CellTools.FixIntersections(ref lines);

            // Iterate through list of lines
            foreach (Line line in lines)
            {
                // Get line, and it's endpoints
                Point3d[] pts = new Point3d[] { line.From, line.To };
                List<int> nodeIndices = new List<int>();
                
                // Loop over end points, being sure to not create the same node twice
                foreach (Point3d pt in pts)
                {
                    int closestIndex = this.Nodes.ClosestIndex(pt);  // find closest node to current pt
                    // If node already exists
                    if (this.Nodes.Count != 0 && this.Nodes[closestIndex].EpsilonEquals(pt, tol))
                        nodeIndices.Add(closestIndex);
                    // If it doesn't exist, add it
                    else
                    {
                        this.Nodes.Add(pt);
                        nodeIndices.Add(this.Nodes.Count - 1);
                    }
                }

                IndexPair nodePair = new IndexPair(nodeIndices[0], nodeIndices[1]);
                // if not duplicate strut, save it
                if (this.NodePairs.Count == 0 || !NodePairs.Contains(nodePair))
                {
                    this.NodePairs.Add(nodePair);
                }
                
            }
        }
        /// <summary>
        /// Scales the unit cell down to unit size (1x1x1) and moves it to the origin
        /// </summary>
        private void NormaliseTopology()
        {
            // We'll build the bounding box as well
            var xRange = new Interval();
            var yRange = new Interval();
            var zRange = new Interval();

            // Get the bounding box size (check for extreme values)
            foreach (Point3d node in this.Nodes)
            {
                if (node.X < xRange.T0) xRange.T0 = node.X;
                if (node.X > xRange.T1) xRange.T1 = node.X;
                if (node.Y < yRange.T0) yRange.T0 = node.Y;
                if (node.Y > yRange.T1) yRange.T1 = node.Y;
                if (node.Z < zRange.T0) zRange.T0 = node.Z;
                if (node.Z > zRange.T1) zRange.T1 = node.Z;
            }

            // move bounding box to origin
            Vector3d toOrigin = new Vector3d(-xRange.T0, -yRange.T0, -zRange.T0);
            this.Nodes.Transform(Transform.Translation(toOrigin));
            // normalise to 1x1x1 bounding box
            this.Nodes.Transform(Transform.Scale(Plane.WorldXY, 1 / xRange.Length, 1 / yRange.Length, 1 / zRange.Length));
        }
        /// <summary>
        /// Checks validity of the unit cell. Note that the cell should be extracted and normalised before running this method.
        /// </summary>
        /// <returns>
        /// -1 : Invalid - opposing faces must have mirror nodes (continuity)
        ///  0 : Invalid - all faces must have at least 1 node (continuity)
        ///  1 : Valid
        /// </returns>
        public int CheckValidity()
        {
            // Set tolerance
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // The check - Opposing faces must be identical
            // Set up the face planes
            Plane[] xy = new Plane[2];
            xy[0] = new Plane(new Point3d(0, 0, 0), Plane.WorldXY.ZAxis);
            xy[1] = new Plane(new Point3d(0, 0, 1), Plane.WorldXY.ZAxis);
            Plane[] yz = new Plane[2];
            yz[0] = new Plane(new Point3d(0, 0, 0), Plane.WorldXY.XAxis);
            yz[1] = new Plane(new Point3d(1, 0, 0), Plane.WorldXY.XAxis);
            Plane[] zx = new Plane[2];
            zx[0] = new Plane(new Point3d(0, 0, 0), Plane.WorldXY.YAxis);
            zx[1] = new Plane(new Point3d(0, 1, 0), Plane.WorldXY.YAxis);

            bool[] minCheck = new bool[3] { false, false, false };  // To make sure each pair of faces has a node lying onit

            // Loop through nodes
            foreach (Point3d node in this.Nodes)
            {
                // Essentially, for every node, we must find it's mirror node on the opposite face
                // First, check if node requires a mirror node, and where that mirror node should be (testPoint)
                Point3d testPoint = Point3d.Unset;

                // XY faces
                if (Math.Abs(xy[0].DistanceTo(node)) < tol)
                {
                    testPoint = new Point3d(node.X, node.Y, xy[1].OriginZ);
                    minCheck[0] = true;
                }
                if (Math.Abs(xy[1].DistanceTo(node)) < tol)
                    testPoint = new Point3d(node.X, node.Y, xy[0].OriginZ);
                // YZ faces
                if (Math.Abs(yz[0].DistanceTo(node)) < tol)
                {
                    testPoint = new Point3d(yz[1].OriginX, node.Y, node.Z);
                    minCheck[1] = true;
                }
                if (Math.Abs(yz[1].DistanceTo(node)) < tol)
                    testPoint = new Point3d(yz[0].OriginX, node.Y, node.Z);
                // ZX faces
                if (Math.Abs(zx[0].DistanceTo(node)) < tol)
                {
                    testPoint = new Point3d(node.X, zx[1].OriginY, node.Z);
                    minCheck[2] = true;
                }
                if (Math.Abs(zx[1].DistanceTo(node)) < tol)
                    testPoint = new Point3d(node.X, zx[0].OriginY, node.Z);

                // Now, check if the mirror node exists
                if (testPoint != Point3d.Unset)
                    if (testPoint.DistanceTo(this.Nodes[this.Nodes.ClosestIndex(testPoint)]) > tol)
                        return -1;
            }

            // Finally, ensure that all faces have a node on it
            if (minCheck[0] == false || minCheck[1] == false || minCheck[2] == false)
                return 0;

            return 1;
        }
        /// <summary>
        /// Defines relative paths of nodes for node pairing.
        /// ASSUMPTION: valid, normalized unit cell
        /// </summary>
        public void FormatTopology()
        {
            // Set tolerance
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Set up boundary planes (struts and nodes on these planes belong to other cells)
            Plane xy = Plane.WorldXY; xy.Translate(new Vector3d(0, 0, 1));
            Plane yz = Plane.WorldYZ; yz.Translate(new Vector3d(1, 0, 0));
            Plane zx = Plane.WorldZX; zx.Translate(new Vector3d(0, 1, 0));

            // Create the relative uvw tree paths, refer to dev docs for better understanding
            foreach (Point3d node in Nodes)
            {
                bool nodeToRemove = true;
                // check top plane first
                if (Math.Abs(xy.DistanceTo(node)) < tol)
                {
                    if (node.DistanceTo(new Point3d(1, 1, 1)) < tol)
                        NodePaths.Add(new int[] { 1, 1, 1, Nodes.ClosestIndex(new Point3d(0, 0, 0)) });            // node belongs to 1,1,1 neighbour
                    else if (Math.Abs(node.X - 1) < tol && Math.Abs(node.Z - 1) < tol)
                        NodePaths.Add(new int[] { 1, 0, 1, Nodes.ClosestIndex(new Point3d(0, node.Y, 0)) });       // node belongs to 1,0,1 neighbour
                    else if (Math.Abs(node.Y - 1) < tol && Math.Abs(node.Z - 1) < tol)
                        NodePaths.Add(new int[] { 0, 1, 1, Nodes.ClosestIndex(new Point3d(node.X, 0, 0)) });       // node belongs to 0,1,1 neighbour
                    else
                        NodePaths.Add(new int[] { 0, 0, 1, Nodes.ClosestIndex(new Point3d(node.X, node.Y, 0)) });  // node belongs to 0,0,1 neighbour
                }
                // check yz boundary plane
                else if (Math.Abs(yz.DistanceTo(node)) < tol)
                {
                    if (Math.Abs(node.X - 1) < tol && Math.Abs(node.Y - 1) < tol)
                        NodePaths.Add(new int[] { 1, 1, 0, Nodes.ClosestIndex(new Point3d(0, 0, node.Z)) });       // node belongs to 1,1,0 neighbour
                    else
                        NodePaths.Add(new int[] { 1, 0, 0, Nodes.ClosestIndex(new Point3d(0, node.Y, node.Z)) });  // node belongs to 1,0,0 neighbour
                }
                // check last boundary plane
                else if (Math.Abs(zx.DistanceTo(node)) < tol)
                    NodePaths.Add(new int[] { 0, 1, 0, Nodes.ClosestIndex(new Point3d(node.X, 0, node.Z)) });      // node belongs to 0,1,0 neighbour
                // if not on those planes, the node belongs to the current cell
                else
                {
                    NodePaths.Add(new int[] { 0, 0, 0, Nodes.IndexOf(node) });
                }
            }

            // now locate any struts that lie on the boundary planes
            List<int> strutsToRemove = new List<int>();
            for (int i = 0; i < this.NodePairs.Count; i++)
            {
                Point3d node1 = this.Nodes[this.NodePairs[i].I];
                Point3d node2 = this.Nodes[this.NodePairs[i].J];

                bool toRemove = false;

                if (Math.Abs(xy.DistanceTo(node1)) < tol && Math.Abs(xy.DistanceTo(node2)) < tol) toRemove = true;
                if (Math.Abs(yz.DistanceTo(node1)) < tol && Math.Abs(yz.DistanceTo(node2)) < tol) toRemove = true;
                if (Math.Abs(zx.DistanceTo(node1)) < tol && Math.Abs(zx.DistanceTo(node2)) < tol) toRemove = true;

                if (toRemove) strutsToRemove.Add(i);
            }
            strutsToRemove.Reverse();
            foreach (int strutToRemove in strutsToRemove) this.NodePairs.RemoveAt(strutToRemove);

        }
        #endregion

    }
}