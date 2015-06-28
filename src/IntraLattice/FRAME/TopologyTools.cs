using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Collections;
using Grasshopper.Kernel.Types.Transforms;

// This is a set of methods used by the frame components
// =====================================================
//      Nothing yet

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class TopologyTools
    {

        /// <summary>
        /// Converts list of lines into a unique list of nodes, and struts as an adjacency list
        /// </summary>
        public static void ExtractTopology(ref List<Curve> lines, ref UnitCell cell)
        {
            // Iterate through list of lines
            foreach (Curve line in lines)
            {
                // Get line, and it's endpoints
                Point3d[] pts = new Point3d[] { line.PointAtStart, line.PointAtEnd };
                List<int> nodeIndex = new List<int>();

                // Loop over end points, being sure to not create the same node twice
                foreach (Point3d endPt in pts)
                {
                    int closestIndex = cell.Nodes.ClosestIndex(endPt);  // find closest node to current pt
                    // If node already exists
                    if (cell.Nodes.Count != 0 && cell.Nodes[closestIndex].DistanceTo(endPt) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                        nodeIndex.Add(closestIndex);
                    // If it doesn't exist, add it
                    else
                    {
                        cell.Nodes.Add(endPt);
                        nodeIndex.Add(cell.Nodes.Count - 1);
                    }
                }

                // Now we save the strut (as pair of node indices)
                cell.Struts.Add(new IndexPair(nodeIndex[0], nodeIndex[1]));
            }
        }

        /// <summary>
        /// Scales the unit cell down to unit size (1x1x1) and moves it to the origin
        /// </summary>
        public static void NormaliseTopology(ref UnitCell cell)
        {
            // We'll build the bounding box as well
            var xRange = new Interval();
            var yRange = new Interval();
            var zRange = new Interval();

            // Get the bounding box size (check for extreme values)
            foreach (Point3d node in cell.Nodes)
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
            cell.Nodes.Transform(Transform.Translation(toOrigin));
            // normalise to 1x1x1 bounding box
            cell.Nodes.Transform(Transform.Scale(Plane.WorldXY, 1 / xRange.Length, 1 / yRange.Length, 1 / zRange.Length));

        }

        /// <summary>
        /// Converts to format that ensures no duplicate nodes or struts are created
        /// ASSUMES VALID TOPOLOGY!!!
        /// </summary>
        public static void FormatTopology(ref UnitCell cell)
        {
            // Set tolerance
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Set up boundary planes (no struts should exist on these planes, and nodes on these planes belong to other cells)
            Plane xy = Plane.WorldXY; xy.Translate(new Vector3d(0, 0, 1));
            Plane yz = Plane.WorldYZ; yz.Translate(new Vector3d(1, 0, 0));
            Plane zx = Plane.WorldZX; zx.Translate(new Vector3d(0, 1, 0));

            // Define node paths in the tree (as mentioned, nodes on the 3 boundary planes belong to other cells in the tree)
            foreach (Point3d node in cell.Nodes)
            {
                // check top plane first
                if (Math.Abs(xy.DistanceTo(node)) < tol)
                {
                    if (node.DistanceTo(new Point3d(1, 1, 1)) < tol)
                        cell.NodePaths.Add(new int[] { 1, 1, 1, cell.Nodes.ClosestIndex(new Point3d(0, 0, 0)) });
                    else if (Math.Abs(node.X - 1) < tol && Math.Abs(node.Z - 1) < tol)
                        cell.NodePaths.Add(new int[] { 1, 0, 1, cell.Nodes.ClosestIndex(new Point3d(0, node.Y, 0)) });
                    else if (Math.Abs(node.Y - 1) < tol && Math.Abs(node.Z - 1) < tol)
                        cell.NodePaths.Add(new int[] { 0, 1, 1, cell.Nodes.ClosestIndex(new Point3d(node.X, 0, 0)) });
                    else
                        cell.NodePaths.Add(new int[] { 0, 0, 1, cell.Nodes.ClosestIndex(new Point3d(node.X, node.Y, 0)) });
                }
                // check next plane
                else if (Math.Abs(yz.DistanceTo(node)) < tol)
                {
                    if (Math.Abs(node.X - 1) < tol && Math.Abs(node.Y - 1) < tol)
                        cell.NodePaths.Add(new int[] { 1, 1, 0, cell.Nodes.ClosestIndex(new Point3d(0, 0, node.Z)) });
                    else
                        cell.NodePaths.Add(new int[] { 1, 0, 0, cell.Nodes.ClosestIndex(new Point3d(0, node.Y, node.Z)) });
                }
                // check third plane
                else if (Math.Abs(zx.DistanceTo(node)) < tol)
                    cell.NodePaths.Add(new int[] { 0, 1, 0, cell.Nodes.ClosestIndex(new Point3d(node.X, 0, node.Z)) });
                // if not on those planes, the node belongs to the current cell
                else
                    cell.NodePaths.Add(new int[] { 0, 0, 0, cell.Nodes.IndexOf(node) });
            }

            // now locate any struts that lie on the boundary planes
            List<int> strutsToRemove = new List<int>();
            for (int i = 0; i < cell.Struts.Count; i++)
            {
                Point3d node1 = cell.Nodes[cell.Struts[i].I];
                Point3d node2 = cell.Nodes[cell.Struts[i].J];

                bool toRemove = false;

                if (Math.Abs(xy.DistanceTo(node1)) < tol && Math.Abs(xy.DistanceTo(node2)) < tol) toRemove = true;
                if (Math.Abs(yz.DistanceTo(node1)) < tol && Math.Abs(yz.DistanceTo(node2)) < tol) toRemove = true;
                if (Math.Abs(zx.DistanceTo(node1)) < tol && Math.Abs(zx.DistanceTo(node2)) < tol) toRemove = true;

                if (toRemove) strutsToRemove.Add(i);
            }
            // discard them (reverse the list because when removing objects from list, all indices larger than the one being removed change by -1)
            strutsToRemove.Reverse();
            foreach (int strutToRemove in strutsToRemove) cell.Struts.RemoveAt(strutToRemove);

        }

        public static void ConformMapping(ref List<GH_Curve> struts, ref GH_Structure<GH_Point> nodeTree, ref GH_Structure<GH_Vector> derivTree, ref UnitCell cell, double[] N, bool morphed)
        {
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair cellStrut in cell.Struts)
                        {
                            // prepare the path of the nodes (path in tree)
                            int[] IRel = cell.NodePaths[cellStrut.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[cellStrut.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2], IRel[3]);
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2], JRel[3]);

                            // make sure both nodes exist (will be false at boundaries)
                            if (nodeTree.PathExists(IPath) && nodeTree.PathExists(JPath))
                            {
                                Point3d node1 = nodeTree[IPath][0].Value;
                                Point3d node2 = nodeTree[JPath][0].Value;

                                // get direction vector from the normalized 'cellNodes'
                                Vector3d directionVector1 = new Vector3d(cell.Nodes[cellStrut.J] - cell.Nodes[cellStrut.I]);
                                directionVector1.Unitize();

                                // if user requested morphing, we need to compute bezier curve struts
                                if (morphed)
                                {
                                    // compute directional derivatives
                                    // we use the du and dv derivatives as the basis for the directional derivative
                                    Vector3d deriv1 = derivTree[IPath][0].Value * directionVector1.X + derivTree[IPath][1].Value * directionVector1.Y;
                                    // same process for node2, but reverse the direction vector
                                    Vector3d directionVector2 = -directionVector1;
                                    Vector3d deriv2 = derivTree[JPath][0].Value * directionVector2.X + derivTree[JPath][1].Value * directionVector2.Y;

                                    // now we have everything we need to build a bezier curve
                                    List<Point3d> controlPoints = new List<Point3d>();
                                    controlPoints.Add(node1); // first control point (vertex)
                                    controlPoints.Add(node1 + deriv1);
                                    controlPoints.Add(node2 + deriv2);
                                    controlPoints.Add(node2); // fourth control point (vertex)
                                    BezierCurve curve = new BezierCurve(controlPoints);

                                    // finally, save the new strut (converted to nurbs)
                                    struts.Add(new GH_Curve(curve.ToNurbsCurve()));
                                }
                                // if user set morph to false, create a simple linear strut
                                else
                                {
                                    LineCurve newStrut = new LineCurve(node1, node2);
                                    struts.Add(new GH_Curve(newStrut));
                                }
                            }
                        }
                    }
                }
            }
        }
    }


    // The UnitCell object
    public class UnitCell
    {
        public Point3dList Nodes = new Point3dList();   // List of unique nodes (as Point3d)
        public List<IndexPair> Struts = new List<IndexPair>();  // List of node index pairs
        public List<int[]> NodePaths = new List<int[]>();   // Relative path of node in tree (u+?, v+?, w+?, ?)
    }

}