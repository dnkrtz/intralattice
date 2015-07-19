using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Intersect;

// This is a set of methods used by the frame components
// =====================================================
//      - ConformMapping
//      - CastDesignSpace
//      - TrimStrut

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class FrameTools
    {
        /// <summary>
        /// Maps cell topology to the node grid created by one of the conform components
        /// </summary>
        public static void ConformMapping(ref List<Curve> struts, ref GH_Structure<GH_Point> nodeTree, ref GH_Structure<GH_Vector> derivTree, ref GH_Structure<GH_Surface> spaceTree, ref UnitCell cell, float[] N, int morphed, double morphTol = 0)
        {
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair cellStrut in cell.StrutNodes)
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

                                // No morphing
                                if ( morphed == 0 )
                                {
                                    LineCurve newStrut = new LineCurve(node1, node2);
                                    struts.Add(newStrut);
                                }
                                // Space morphing
                                else if ( morphed == 1 )
                                {
                                    if (u == N[0] || v == N[1]) continue;

                                    GH_Path spacePath = new GH_Path(u, v);
                                    Surface ss1 = spaceTree[spacePath][0].Value.Surfaces[0]; // uv cell space, as pair of subsurfaces (subsurface of the full surface)
                                    Surface ss2 = spaceTree[spacePath][1].Value.Surfaces[0]; // this surface will be null if 
                                    ss1.SetDomain(0, new Interval(0, 1));
                                    ss1.SetDomain(1, new Interval(0, 1));
                                    ss2.SetDomain(0, new Interval(0, 1));
                                    ss2.SetDomain(1, new Interval(0, 1));

                                    // Discretize the unit cell line for morph mapping
                                    int ptCount = 10 + 1;
                                    //int divNumber = (int)(node1.DistanceTo(node2) / morphTol);    // number of discrete segments
                                    var templatePts = new List<Point3d>();   // unitized cell points (x,y of these points are u,v of sub-surface)
                                    Line templateLine = new Line(cell.Nodes[cellStrut.I], cell.Nodes[cellStrut.J]);
                                    for (int ptIndex=0; ptIndex<=ptCount; ptIndex++)
                                        templatePts.Add(templateLine.PointAt(ptIndex / (double)ptCount));                                    

                                    // We will map each template point to its uvw cell-space
                                    var controlPoints = new List<Point3d>();    // interpolation points in space
                                    
                                    foreach (Point3d tempPt in templatePts)
                                    {
                                        Point3d surfPt1, surfPt2;
                                        Vector3d[] surfDerivs1, surfDerivs2;
                                        ss1.Evaluate(tempPt.X, tempPt.Y, 0, out surfPt1, out surfDerivs1);
                                        ss2.Evaluate(tempPt.X, tempPt.Y, 0, out surfPt2, out surfDerivs2);
                                        Vector3d wVect = surfPt2 - surfPt1;

                                        Point3d uvwPt = surfPt1 + wVect * (w + tempPt.Z) / N[2];
                                        controlPoints.Add(uvwPt);
                                    }

                                    Curve curve = Curve.CreateInterpolatedCurve(controlPoints, 3);

                                    //Curve curve = NurbsCurve.Create(false, controlPoints.Count - 1, controlPoints);
                                    // finally, save the new strut
                                    struts.Add(curve);
                                }
                                // Bezier morphing
                                else if ( morphed == 2 )
                                {
                                    // get direction vector from the normalized 'cellNodes'
                                    Vector3d directionVector1 = new Vector3d(cell.Nodes[cellStrut.J] - cell.Nodes[cellStrut.I]);
                                    directionVector1.Unitize();

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
                                    struts.Add(curve.ToNurbsCurve());
                                }
                            
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Maps cell topology to the node grid and trims to the design space
        /// </summary>
         public static void UniformMapping(ref List<Curve> struts, ref GH_Structure<GH_Point> nodeTree, ref GH_Structure<GH_Boolean> stateTree, ref UnitCell cell, float[] N, Brep brepDesignSpace, Mesh meshDesignSpace)
        {
            // nodes that must be removed from the data structure
            var nodesToRemove = new List<GH_Path>();

            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair cellStrut in cell.StrutNodes)
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

                                // Determine inside/outside state of both nodes
                                bool[] isInside = new bool[2];
                                isInside[0] = stateTree[IPath][0].Value;
                                isInside[1] = stateTree[JPath][0].Value;

                                // If neither node is inside, remove them and skip to next loop
                                if (!isInside[0] && !isInside[1])
                                {
                                    nodesToRemove.Add(IPath);
                                    nodesToRemove.Add(JPath);
                                    continue;
                                }
                                // If both nodes are inside, add full strut
                                else if (isInside[0] && isInside[1])
                                    struts.Add(new LineCurve(node1, node2));
                                // Else, strut requires trimming
                                else
                                {
                                    // We are going to find the intersection point with the design space
                                    Point3d[] intersectionPts = null;
                                    LineCurve testLine = null;

                                    // If brep design space
                                    if (brepDesignSpace != null)
                                    {
                                        Curve[] overlapCurves = null;   // dummy variable for CurveBrep call
                                        LineCurve strutToTrim = new LineCurve(node1, node2);
                                        // find intersection point
                                        Intersection.CurveBrep(strutToTrim, brepDesignSpace, Rhino.RhinoMath.SqrtEpsilon, out overlapCurves, out intersectionPts);
                                    }
                                    // If mesh design space
                                    else if (meshDesignSpace != null)
                                    {
                                        int[] faceIds;  // dummy variable for MeshLine call
                                        Line strutToTrim = new Line(node1, node2);
                                        // find intersection point
                                        intersectionPts = Intersection.MeshLine(meshDesignSpace, strutToTrim, out faceIds);
                                    }

                                    // Now, if an intersection point was found, trim the strut
                                    if (intersectionPts.Length > 0)
                                    {
                                        testLine = FrameTools.TrimStrut(ref nodeTree, ref stateTree, ref nodesToRemove, IPath, JPath, intersectionPts[0], isInside);
                                        // if the strut was succesfully trimmed, add it to the list
                                        if (testLine != null) struts.Add(testLine);
                                    }

                                }
                            }
                        }
                    }
                }
            }

            foreach (GH_Path nodeToRemove in nodesToRemove)
            {
                if (nodeTree.PathExists(nodeToRemove))
                {
                    if (nodeTree[nodeToRemove].Count > 1)  // if node is a swap node (replaced by intersection pt)
                    {
                        nodeTree[nodeToRemove].RemoveAt(0);
                        stateTree[nodeToRemove].RemoveAt(0);
                    }
                    else if (!stateTree[nodeToRemove][0].Value) // if node is outside
                        nodeTree.RemovePath(nodeToRemove);
                }
            }

        }

        public static bool CastDesignSpace(ref GeometryBase designSpace, ref Brep brepDesignSpace, ref Mesh meshDesignSpace)
        {
            //    If brep design space, cast as such
            if (designSpace.ObjectType == ObjectType.Brep)
                brepDesignSpace = (Brep)designSpace;
            //    If mesh design space, cast as such
            else if (designSpace.ObjectType == ObjectType.Mesh)
                meshDesignSpace = (Mesh)designSpace;
            //    If solid surface, convert to brep
            else if (designSpace.ObjectType == ObjectType.Surface)
            {
                Surface testSpace = (Surface)designSpace;
                if (testSpace.IsSolid) brepDesignSpace = testSpace.ToBrep();
            }
            //    Else the design space is unacceptable
            else
                return false;

            return true;
        }

        public static LineCurve TrimStrut(ref GH_Structure<GH_Point> nodeTree, ref GH_Structure<GH_Boolean> stateTree, ref List<GH_Path> nodesToRemove, GH_Path IPath, GH_Path JPath, Point3d intersectionPt, bool[] isInside)
        {
            GH_Path[] paths = new GH_Path[] { IPath, JPath };
            Point3d[] nodes = new Point3d[] { nodeTree[IPath][0].Value, nodeTree[JPath][0].Value };
            LineCurve testStrut = new LineCurve(new Line(nodes[0], nodes[1]), 0, 1);  // set line, with curve parameter domain [0,1]

            // We only create strut if the trimmed strut is a certain length
            double strutLength = nodes[0].DistanceTo(nodes[1]);

            for (int index=0; index<2; index++ )
            {
                if (isInside[index])
                {
                    double testLength = intersectionPt.DistanceTo(nodes[index]);
                    // if trimmed length is less than 10% of full strut length
                    if (testLength > strutLength * 0.1)
                    {
                        nodeTree[paths[(index + 1) % 2]].Add(new GH_Point(intersectionPt)); // the intersection point will replace the outside node in the tree
                        stateTree[paths[(index + 1) % 2]].Add(new GH_Boolean(true));
                        return new LineCurve(nodes[index], intersectionPt);
                    }
                        
                }
                else if (!nodesToRemove.Contains(paths[index]))
                {
                    nodesToRemove.Add(paths[index]);
                }
                    
            }
           
            return null;
        }


    }
}