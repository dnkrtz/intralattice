using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Intersect;
using Rhino.Collections;
using Rhino;
using IntraLattice.Properties;
using Grasshopper;
using IntraLattice.CORE.Data;
using IntraLattice.CORE.Components;
using IntraLattice.CORE.Helpers;

// Summary:     This component generates a (u,v,w) lattice grid between a surface and an axis
// ===============================================================================
// Details:     - The axis can be an open curve or a closed curve. Of course, it may also be a straight line.
//              - The surface does not need to loop a full 360 degrees around the axis.
//              - Our implementation assumes that the axis is a set of U parameters.
//              - The flipUV input allows the user to swap U and V parameters of the surface.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components
{
    public class ConformSA : GH_Component
    {
        public ConformSA()
            : base("Conform Surface-Axis", "ConformSA",
                "Generates a conforming lattice between a surface and an axis.",
                "IntraLattice2", "Frame")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("Surface", "Surf", "Surface to conform to", GH_ParamAccess.item);
            pManager.AddCurveParameter("Axis", "A", "Axis (may be curved)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip UV", "FlipUV", "Flip the U and V parameters on the surface", GH_ParamAccess.item, false); // default value is true
            pManager.AddIntegerParameter("Number u", "Nu", "Number of unit cells (u)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number v", "Nv", "Number of unit cells (v)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number w", "Nw", "Number of unit cells (w)", GH_ParamAccess.item, 5);
            pManager.AddBooleanParameter("Morph", "Morph", "If true, struts are morphed to the space as curves.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Strut curve network", GH_ParamAccess.list);
            pManager.AddPointParameter("Nodes", "Nodes", "Lattice Nodes", GH_ParamAccess.list);
            pManager.HideParameter(1);  // Do not display the 'Nodes' output points
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate inputs
            var topology = new List<Line>();
            Surface surface = null;
            Curve axis = null;
            bool flipUV = false;
            int nU = 0;
            int nV = 0;
            int nW = 0;
            bool morphed = false;

            if (!DA.GetDataList(0, topology)) { return; }
            if (!DA.GetData(1, ref surface)) { return; }
            if (!DA.GetData(2, ref axis)) { return; }
            if (!DA.GetData(3, ref flipUV)) { return; }
            if (!DA.GetData(4, ref nU)) { return; }
            if (!DA.GetData(5, ref nV)) { return; }
            if (!DA.GetData(6, ref nW)) { return; }
            if (!DA.GetData(7, ref morphed)) { return; }

            if (topology.Count < 2) { return; }
            if (!surface.IsValid) { return; }
            if (!axis.IsValid) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // 2. Initialize the node tree, derivative tree and morphed space tree
            var nodeTree = new DataTree<Point3d>();                                 // will contain lattice nodes
            var spaceTree = new DataTree<GeometryBase>();                           // will contain the morphed uv spaces (as surface-surface, surface-axis or surface-point)           

            // 3. Flip the UV parameters if specified
            if (flipUV) surface = surface.Transpose();

            // 4. Package the number of cells in each direction into an array
            float[] N = new float[3] { nU, nV, nW };

            // 5. Normalize the UV-domain
            Interval normalDomain = new Interval(0, 1);
            surface.SetDomain(0, normalDomain); // surface u-direction
            surface.SetDomain(1, normalDomain); // surface v-direction
            axis.Domain = normalDomain; // axis (u-direction)

            // 6. Prepare normalized/formatted unit cell topology
            var cell = new UnitCell();
            cell.ExtractTopology(topology); // fixes intersections, and formats lines to the UnitCell object
            cell.NormaliseTopology();       // normalizes the unit cell (scaled to unit size and moved to origin)
            cell.FormatTopology();          // sets up paths for inter-cell nodes

            // 7. Divide axis into equal segments, get curve parameters
            List<double> curveParams = new List<double>(axis.DivideByCount((int)N[0], true));
            double uStep = curveParams[1] - curveParams[0];
            //    If axis is closed curve, add last parameter to close the loop
            if (axis.IsClosed) curveParams.Add(0);

            // 8. Let's create the actual lattice nodes now
            //
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    // create grid points on and between surface-axis
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // this loop maps each node in the cell onto the UV-surface map and axis (U)
                        for (int i = 0; i < cell.Nodes.Count; i++)
                        {
                            // if the node belongs to another cell (i.e. it's relative path points outside the current cell)
                            if (cell.NodePaths[i][0] + cell.NodePaths[i][1] + cell.NodePaths[i][2] > 0)
                                continue;

                            // local node position within cell
                            double usub = cell.Nodes[i].X; // u-position within unit cell
                            double vsub = cell.Nodes[i].Y; // v-position within unit cell
                            double wsub = cell.Nodes[i].Z; // w-position within unit cell

                            // these conditionals enforce the boundary, no nodes are created beyond the upper boundary
                            if (u == N[0] && usub != 0) continue;
                            if (v == N[1] && vsub != 0) continue;
                            if (w == N[2] && wsub != 0) continue;

                            // evaluate the point on the axis
                            Point3d pt1;
                            Point3d pt2; Vector3d[] derivatives; // initialize for surface 2

                            // evaluate point and its derivatives on the axis and the surface
                            pt1 = axis.PointAt(curveParams[u] + usub / N[0]);
                            surface.Evaluate((u + usub) / N[0], (v + vsub) / N[1], 2, out pt2, out derivatives);

                            // create vector joining the two points (this is our w-range)
                            Vector3d wVect = pt2 - pt1;

                            GH_Path treePath = new GH_Path(u, v, w, i);    // u,v,w is the cell grid. the last index is for different nodes in each cell.

                            // create the node, accounting for the position along the w-direction
                            Point3d newPt = pt1 + wVect * (w + wsub) / N[2];
                            nodeTree.Add(newPt, treePath);

                        }
                    }

                    // Define the uv space map
                    if (u < N[0] && v < N[1])
                    {
                        GH_Path spacePath = new GH_Path(u, v);
                        var uInterval = new Interval((u) / N[0], (u + 1) / N[0]);                   // set trimming interval
                        var vInterval = new Interval((v) / N[1], (v + 1) / N[1]);
                        Surface ss1 = surface.Trim(uInterval, vInterval);                           // create sub-surface
                        Curve ss2 = axis.Trim(uInterval);                                           // create sub-axis
                        ss1.SetDomain(0, normalDomain); ss1.SetDomain(1, normalDomain);     // normalize domain
                        ss2.Domain = normalDomain;
                        // Save to the space tree
                        spaceTree.Add(ss1, spacePath);
                        spaceTree.Add(ss2, spacePath);
                    }
                }
            }

            // 9. Generate the struts
            //    Simply loop through all unit cells, and enforce the cell topology (using cellStruts: pairs of node indices)
            var struts = new List<Curve>();
            var nodes = new Point3dList();
            struts = FrameTools.ConformMapping(nodeTree, spaceTree, cell, N, morphed);
            struts = FrameTools.CleanNetwork(struts, out nodes);

            // 10. Set output
            DA.SetDataList(0, struts);
            DA.SetDataList(1, nodes);

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
                //return Resources.circle3;
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
            get { return new Guid("{e0e8a858-66bd-4145-b173-23dc2e247206}"); }
        }
    }
}
