using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;
using Rhino.DocObjects;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

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
        public static void ConformMapping(ref List<Curve> struts, ref GH_Structure<GH_Point> nodeTree, ref GH_Structure<GH_Vector> derivTree, ref UnitCell cell, float[] N, bool morphed)
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

                                // get direction vector from the normalized 'cellNodes'
                                Vector3d directionVector1 = new Vector3d(cell.Nodes[cellStrut.J] - cell.Nodes[cellStrut.I]);
                                directionVector1.Unitize();

                                // if user requested morphing, we need to compute bezier curve struts
                                if (morphed && directionVector1.Z==0)
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
                                    struts.Add(curve.ToNurbsCurve());
                                }
                                // if user set morph to false, create a simple linear strut
                                else
                                {
                                    LineCurve newStrut = new LineCurve(node1, node2);
                                    struts.Add(newStrut);
                                }
                            }
                        }
                    }
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