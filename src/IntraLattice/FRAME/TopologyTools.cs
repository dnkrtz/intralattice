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
        /// Converts a list of lines
        /// </summary>
        public static void NormaliseTopology(ref Point3dList nodes)
        {
            // We'll build the bounding box as well
            var xRange = new Interval();
            var yRange = new Interval();
            var zRange = new Interval();

            // Get the bounding box size (check for extreme values)
            foreach (Point3d node in nodes)
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
            nodes.Transform(Transform.Translation(toOrigin));
            // normalise to 1x1x1 bounding box
            nodes.Transform(Transform.Scale(Plane.WorldXY, 1 / xRange.Length, 1 / yRange.Length, 1 / zRange.Length));

        }

        /// <summary>
        /// Converts list of lines into a list of unique nodes and a list of struts (struts as pairs of node indices)
        /// </summary>
        public static void Topologize(ref List<Curve> lines, ref Point3dList nodes, ref List<IndexPair> struts)
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
                    int closestIndex = nodes.ClosestIndex(endPt);  // find closest node to current pt
                    // If node already exists
                    if (nodes.Count != 0 && nodes[closestIndex].DistanceTo(endPt) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                        nodeIndex.Add(closestIndex);
                    // If it doesn't exist, add it
                    else
                    {
                        nodes.Add(endPt);
                        nodeIndex.Add(nodes.Count - 1);
                    }
                }

                // Now we save the strut (as pair of node indices)
                struts.Add(new IndexPair(nodeIndex[0], nodeIndex[1]));
            }
        }

        public static void ConformMapping(ref List<GH_Curve> struts, ref GH_Structure<GH_Point> nodeTree, ref GH_Structure<GH_Vector> derivTree, List<IndexPair> cellStruts, Point3dList cellNodes, double[] N, bool morphed)
        {
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair cellStrut in cellStruts)
                        {
                            // prepare the path of the nodes (path in tree)
                            GH_Path startPath = new GH_Path(u, v, w, cellStrut.I);
                            GH_Path endPath = new GH_Path(u, v, w, cellStrut.J);

                            // make sure both nodes exist (will be false at boundaries)
                            if (nodeTree.PathExists(startPath) && nodeTree.PathExists(endPath))
                            {
                                Point3d node1 = nodeTree[startPath][0].Value;
                                Point3d node2 = nodeTree[endPath][0].Value;

                                // get direction vector from the normalized 'cellNodes'
                                Vector3d directionVector1 = new Vector3d(cellNodes[cellStrut.J] - cellNodes[cellStrut.I]);
                                directionVector1.Unitize();

                                // if user requested morphing, we need to compute bezier curve struts
                                if (morphed)
                                {
                                    // compute directional derivatives
                                    // we use the du and dv derivatives as the basis for the directional derivative
                                    Vector3d deriv1 = derivTree[startPath][0].Value * directionVector1.X + derivTree[startPath][1].Value * directionVector1.Y;
                                    // same process for node2, but reverse the direction vector
                                    Vector3d directionVector2 = -directionVector1;
                                    Vector3d deriv2 = derivTree[endPath][0].Value * directionVector2.X + derivTree[endPath][1].Value * directionVector2.Y;

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
}