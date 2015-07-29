using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Intersect;
using Grasshopper;


// Summary:     This class contains a set of methods used by the frame components
// ===============================================================================
// Methods:     ConformMapping (written by Aidan)   - Generates conforming wire lattice for a (u,v,w,i) node grid.
//              UniformMapping(written by Aidan)    - Generates trimmed wire lattice for a (u,v,w,i) node grid.
//              TrimStrut (written by Aidan)        - Trims strut at an intersection point and keeps the trimmed strut that is inside a design space.
//              CastDesignSpace (written by Aidan)  - Casts GeometryBase design space to a Brep or Mesh.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class FrameTools
    {
        /// <summary>
        /// Maps cell topology to the node grid created by one of the conform components, with 3 morphing options
        /// ============================================================================
        /// 0) No morphing
        /// 1) Space morphing (discretization of the struts as a set of points, which are mapped to the uvw cell spaces, and interpolated as NURBS-curves)
        /// 2) Bezier morphing (uses interpolated directional surface derivatives to morph the struts as Bezier curves)
        /// ============================================================================                    
        /// </summary>
        public static void ConformMapping(ref List<Curve> struts, ref DataTree<Point3d> nodeTree, ref DataTree<Vector3d> derivTree, ref DataTree<GeometryBase> spaceTree, ref UnitCell cell, float[] N, int morphed, double morphTol = 0)
        {
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
                            if (nodeTree.PathExists(IPath) && nodeTree.PathExists(JPath))
                            {
                                Point3d node1 = nodeTree[IPath, 0];
                                Point3d node2 = nodeTree[JPath, 0];

                                // No morphing
                                if ( morphed == 0 )
                                {
                                    LineCurve newStrut = new LineCurve(node1, node2);
                                    struts.Add(newStrut);
                                }
                                // Space morphing
                                else if ( morphed == 1 )
                                {
                                    GH_Path spacePath;

                                    // if we're at a boundary, we need to use the previous space
                                    if (u == N[0] && v == N[1])
                                        spacePath = new GH_Path(u - 1, v - 1);
                                    else if (u == N[0])
                                        spacePath = new GH_Path(u - 1, v);
                                    else if (v == N[1])
                                        spacePath = new GH_Path(u, v-1);
                                    else
                                        spacePath = new GH_Path(u, v);

                                    GeometryBase ss1 = spaceTree[spacePath, 0]; // retrieve uv cell space (will be casted in the tempPt loop)
                                    GeometryBase ss2 = spaceTree[spacePath, 1];

                                    // Discretize the unit cell line for morph mapping
                                    int ptCount = 16;
                                    //int divNumber = (int)(node1.DistanceTo(node2) / morphTol);    // number of discrete segments
                                    var templatePts = new List<Point3d>();   // unitized cell points (x,y of these points are u,v of sub-surface)
                                    Line templateLine = new Line(cell.Nodes[cellStrut.I], cell.Nodes[cellStrut.J]);
                                    for (int ptIndex=0; ptIndex<=ptCount; ptIndex++)
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
                                        if (u == N[0]) uParam = 1-uParam;
                                        if (v == N[1]) vParam = 1-vParam;


                                        // Now, we will map the template point to the uvw-space
                                        ((Surface)ss1).Evaluate(uParam, vParam, 0, out surfPt, out surfDerivs);
                                        Vector3d wVect = Vector3d.Unset;
                                        switch (ss2.ObjectType)
                                        {
                                            case ObjectType.Point:      // point
                                                wVect = ((Point)ss2).Location - surfPt;;
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
                                    Vector3d deriv1 = derivTree[IPath, 0] * directionVector1.X + derivTree[IPath, 1] * directionVector1.Y;
                                    // same process for node2, but reverse the direction vector
                                    Vector3d directionVector2 = -directionVector1;
                                    Vector3d deriv2 = derivTree[JPath, 0] * directionVector2.X + derivTree[JPath, 1] * directionVector2.Y;

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
        /// =================================================================
        /// - stateTree contains information about whether a node in the nodeTree is inside/outside the design space.
        /// - Trimming is performed by the TrimStrut method, note that it's set up so that the intersection pt replaces the external node
        /// - We remove the external nodes (the intersection nodes will replace them, since they are appended to the path in the trimStrut method)
        /// =================================================================
        /// </summary>
        public static void UniformMapping(ref List<Curve> struts, ref DataTree<Point3d> nodeTree, ref DataTree<Boolean> stateTree, ref UnitCell cell, GeometryBase designSpace, int spaceType, float[] N, double tol)
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
                        foreach (IndexPair cellStrut in cell.NodePairs)
                        {
                            // prepare the path of the nodes (path in tree)
                            int[] IRel = cell.NodePaths[cellStrut.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[cellStrut.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2], IRel[3]);
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2], JRel[3]);

                            // make sure both nodes exist (will be false at boundaries)
                            if (nodeTree.PathExists(IPath) && nodeTree.PathExists(JPath))
                            {
                                Point3d node1 = nodeTree[IPath, 0];
                                Point3d node2 = nodeTree[JPath, 0];

                                // Determine inside/outside state of both nodes
                                bool[] isInside = new bool[2];
                                isInside[0] = stateTree[IPath, 0];
                                isInside[1] = stateTree[JPath, 0];

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
                                    LineCurve strutToTrim = null;

                                    switch (spaceType)
                                    {
                                        // Brep design space
                                        case 1: 
                                            Curve[] overlapCurves = null;   // dummy variable for CurveBrep call
                                            strutToTrim = new LineCurve(node1, node2);
                                            // find intersection point
                                            Intersection.CurveBrep(strutToTrim, (Brep)designSpace, Rhino.RhinoMath.SqrtEpsilon, out overlapCurves, out intersectionPts);
                                            break;
                                        // Mesh design space
                                        case 2:
                                            int[] faceIds;  // dummy variable for MeshLine call
                                            strutToTrim = new LineCurve(node1, node2);
                                            // find intersection point
                                            intersectionPts = Intersection.MeshLine((Mesh)designSpace, strutToTrim.Line, out faceIds);
                                            break;
                                        // Solid surface design space
                                        case 3:
                                            overlapCurves = null;   // dummy variable for CurveBrep call
                                            strutToTrim = new LineCurve(node1, node2);
                                            // find intersection point
                                            Intersection.CurveBrep(strutToTrim, ((Surface)designSpace).ToBrep(), Rhino.RhinoMath.SqrtEpsilon, out overlapCurves, out intersectionPts);
                                            break;
                                    }

                                    LineCurve testLine = null;
                                    // Now, if an intersection point was found, trim the strut
                                    if (intersectionPts.Length > 0)
                                    {
                                        testLine = FrameTools.TrimStrut(ref nodeTree, ref stateTree, ref nodesToRemove, IPath, JPath, intersectionPts[0], isInside, tol);
                                        // if the strut was succesfully trimmed, add it to the list
                                        if (testLine != null) struts.Add(testLine);
                                    }

                                }
                            }
                        }
                    }
                }
            }

            // Remove the external nodes, and replace them with the intersection nodes
            foreach (GH_Path nodeToRemove in nodesToRemove)
            {
                if (nodeTree.PathExists(nodeToRemove))
                {
                    // In the TrimStrut method, we append the intersection point at the path of the node to remove
                    // Thus, if there is a second item at this path...
                    // We know it's a swap node
                    if (nodeTree.ItemExists(nodeToRemove, 1))
                    {
                        Point3d swapNode = nodeTree[nodeToRemove, 1];
                        nodeTree.RemovePath(nodeToRemove);      // remove full node path (both items)
                        nodeTree.Add(swapNode, nodeToRemove);   // create new node at same path
                        stateTree.RemovePath(nodeToRemove);
                        stateTree.Add(true, nodeToRemove);      // new node is necessarily "inside"
                    }
                    // If it's not a swap node, but it's outside, we just remove it
                    else if (!stateTree[nodeToRemove, 0]) // if node is outside
                        nodeTree.RemovePath(nodeToRemove);
                }
            }

        }

        /// <summary>
        /// Trims strut with known intersection point, returning  the trimmed LineCurve which is inside the space
        /// =================================================================
        /// - Intersection point and information about inside/outside state are passed to this method, to know where to trim and which side to keep.
        /// =================================================================
        /// </summary>
        public static LineCurve TrimStrut(ref DataTree<Point3d> nodeTree, ref DataTree<Boolean> stateTree, ref List<GH_Path> nodesToRemove, GH_Path IPath, GH_Path JPath, Point3d intersectionPt, bool[] isInside, double trimTolerance)
        {
            GH_Path[] paths = new GH_Path[] { IPath, JPath };
            Point3d[] nodes = new Point3d[] { nodeTree[IPath, 0], nodeTree[JPath, 0] };
            LineCurve testStrut = new LineCurve(new Line(nodes[0], nodes[1]), 0, 1);  // set line, with curve parameter domain [0,1]

            // We only create strut if the trimmed strut is a certain length
            double strutLength = nodes[0].DistanceTo(nodes[1]);

            // Loop through the 2 nodes
            for (int index=0; index<2; index++ )
            {
                // if current node is inside
                if (isInside[index])
                {
                    double testLength = intersectionPt.DistanceTo(nodes[index]);
                    // if trimmed length is greater than trim tolerance (i.e. minimum strut length)
                    if (testLength > trimTolerance)
                    {
                        nodeTree.Add(intersectionPt, paths[(index + 1) % 2]); // the intersection point will replace the outside node in the tree
                        stateTree.Add(true, paths[(index + 1) % 2]);
                        return new LineCurve(nodes[index], intersectionPt);
                    }
                        
                }
                // otherwise, current node is outside and should be removed
                // if it's not already in the nodesToRemove list, add it
                else if (!nodesToRemove.Contains(paths[index]))
                {
                    nodesToRemove.Add(paths[index]);
                }
                    
            }
           
            return null;
        }

        /// <summary>
        /// Casts a GeometryBase design space to a brep or a mesh.
        /// </summary>
        public static int CastDesignSpace(ref GeometryBase designSpace)
        {
            // Types: 0-invalid, 1-brep, 2-mesh, 3-solid surface
            int type = 0;

            if (designSpace.ObjectType == ObjectType.Brep)
                type = 1;
            else if (designSpace.ObjectType == ObjectType.Mesh)
                type = 2;
            else if (designSpace.ObjectType == ObjectType.Surface && ((Surface)designSpace).IsSolid)
                type = 3;

            return type;
        }


    }
}