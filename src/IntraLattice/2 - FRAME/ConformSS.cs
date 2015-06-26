using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;
using Rhino.Collections;
using Rhino;

// This component generates a conformal lattice grid between two surfaces.
// =======================================================================
// Assumption : The surfaces are oriented in the same direction (for UV-Map indices)

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class GridConformSS : GH_Component
    {
        public GridConformSS()
            : base("Conform Surface-Surface", "ConformSS",
                "Generates a conforming point grid between two surfaces.",
                "IntraLattice2", "Grid")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("Surface 1", "S1", "First bounding surface", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface 2", "S2", "Second bounding surface", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip UV", "FlipUV", "Flip the UV parameters (for alignment purposes)", GH_ParamAccess.item, false); // default value is false
            pManager.AddNumberParameter("Number u", "Nu", "Number of unit cells (u)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number v", "Nv", "Number of unit cells (v)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number w", "Nw", "Number of unit cells (w)", GH_ParamAccess.item, 5);
            pManager.AddBooleanParameter("Morph", "Morph", "If true, struts will morph to the design space (as bezier curves)", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Morph Factor", "MF", "Division factor for bezier vectors (recommended: 2.0-3.0)", GH_ParamAccess.item, 3);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "Grid", "Point grid", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Strut lines", "Lines", "Strut line network", GH_ParamAccess.list);
            pManager.AddVectorParameter("t", "t", "t", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables
            List<Curve> topology = new List<Curve>();
            Surface s1 = null;
            Surface s2 = null;
            bool flipUV = false;
            double nU = 0;
            double nV = 0;
            double nW = 0;
            bool morphed = false;
            double morphFactor = 0;

            // 2. Attempt to fetch data
            if (!DA.GetDataList(0, topology)) { return; }
            if (!DA.GetData(1, ref s1)) { return; }
            if (!DA.GetData(2, ref s2)) { return; }
            if (!DA.GetData(3, ref flipUV)) { return; }
            if (!DA.GetData(4, ref nU)) { return; }
            if (!DA.GetData(5, ref nV)) { return; }
            if (!DA.GetData(6, ref nW)) { return; }
            if (!DA.GetData(7, ref morphed)) { return; }
            if (!DA.GetData(8, ref morphFactor)) { return; }

            // 3. Validate data
            if (!s1.IsValid) { return; }
            if (!s2.IsValid) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // 4. Initialize the grid tree and derivatives tree
            var nodeTree = new GH_Structure<GH_Point>();     // will contain lattice nodes
            var derivTree = new GH_Structure<GH_Vector>();   // will contain derivatives (du,dv) in a parallel tree

            // 5. Flip the UV parameters a surface if specified
            if (flipUV) s1 = s1.Transpose();
            
            // 6. Package the number of cells in each direction into an array
            double[] N = new double[3] { nU, nV, nW };

            // 7. Normalize the UV-domain
            Interval normalizedDomain = new Interval(0,1);
            s1.SetDomain(0, normalizedDomain); // s1 u-direction
            s1.SetDomain(1, normalizedDomain); // s1 v-direction
            s2.SetDomain(0, normalizedDomain); // s2 u-direction
            s2.SetDomain(1, normalizedDomain); // s2 v-direction

            // 8. Prepare normalized unit cell topology
            var cellNodes = new Point3dList();
            var cellStruts = new List<IndexPair>();
            TopologyTools.Topologize(ref topology, ref cellNodes, ref cellStruts);  // converts list of lines into an adjacency list format (cellNodes and cellStruts)
            TopologyTools.NormaliseTopology(ref cellNodes); // normalizes the unit cell (scaled to unit size and moved to origin)

            // 9. Map nodes to design space
            //    Loop through the uvw cell grid
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    // this loop maps each node in the cell onto the UV-surface maps
                    for (int nodeIndex = 0; nodeIndex < cellNodes.Count; nodeIndex++)
                    {
                        Point3d pt1; Vector3d[] derivatives1; // initialize for surface 1
                        Point3d pt2; Vector3d[] derivatives2; // initialize for surface 2

                        double usub = cellNodes[nodeIndex].X; // u-position within unit cell
                        double vsub = cellNodes[nodeIndex].Y; // v-position within unit cell
                        double wsub = cellNodes[nodeIndex].Z; // w-position within unit cell

                        // evaluate point and its derivatives on both surfaces
                        s1.Evaluate((u+usub) / N[0], (v+vsub) / N[1], 2, out pt1, out derivatives1);
                        s2.Evaluate((u+usub) / N[0], (v+vsub) / N[1], 2, out pt2, out derivatives2);

                        // create vector joining the two points (this is our w-range)
                        Vector3d wVect = pt2 - pt1;

                        // create grid points on and between surfaces
                        for (int w = 0; w <= N[2]; w++)
                        {
                            GH_Path treePath = new GH_Path(u, v, w, nodeIndex);    // u,v,w is the cell grid. the last index is for different nodes in each cell.

                            // these conditionals enforce the boundary, no nodes are created beyond the upper boundary
                            if ( u==N[0] && usub!=0) continue;
                            if ( v==N[1] && vsub!=0) continue;
                            if ( w==N[2] && wsub!=0) continue;

                            // create the node, accounting for the position along the w-direction
                            Point3d newPt = pt1 + wVect * (w + wsub) / N[2];
                            nodeTree.Append(new GH_Point(newPt), treePath);

                            // for each of the 2 directional directives (du and dv)
                            for (int derivIndex = 0; derivIndex < 2; derivIndex++)
                            {
                                // compute the interpolated derivative (need interpolation for in-between surfaces)
                                double interpolationFactor = (w+wsub) / N[2];
                                Vector3d deriv = derivatives1[derivIndex] + interpolationFactor * (derivatives2[derivIndex] - derivatives1[derivIndex]);
                                // this division scales the derivatives (gives better control of the bezier curves)
                                deriv = deriv / (morphFactor * N[derivIndex]); 
                                derivTree.Append(new GH_Vector(deriv), treePath);
                            }
                        }
                    }
                }
            }

            // 10. Generate the struts
            //     Simply loop through all unit cells, and enforce the cell topology (using cellStruts: pairs of node indices)
            var struts = new List<GH_Curve>();
            //
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

                                // if user requested morphing, we need to compute bezier curve struts
                                if (morphed)
                                {
                                    // compute directional derivatives
                                    // get direction vector from normalized cellNodes
                                    Vector3d directionVector1 = new Vector3d(cellNodes[cellStrut.J] - cellNodes[cellStrut.I]);
                                    directionVector1.Unitize();
                                    // we use the du and dv derivatives as the basis for the directional derivative
                                    Vector3d deriv1 = derivTree[startPath][0].Value * directionVector1.X + derivTree[startPath][1].Value * directionVector1.Y;
                                    // same process for node2, but reverse the direction vector
                                    Vector3d directionVector2 = - directionVector1;
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

            // 11. Set output
            DA.SetDataTree(0, nodeTree);
            DA.SetDataList(1, struts);

        }

        // Conform components are in second slot of the grid category
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{ac0814b4-00e7-4efb-add5-e845a831c6da}"); }
        }
    }
}
