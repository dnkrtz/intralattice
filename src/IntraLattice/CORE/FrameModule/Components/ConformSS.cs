using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;
using Rhino.Collections;
using Rhino;
using Grasshopper;
using IntraLattice.Properties;
using IntraLattice.CORE.CellModule;
using IntraLattice.CORE.FrameModule.Data;

// Summary:     This component generates a (u,v,w) lattice between two surfaces
// ===============================================================================
// Details:     - 
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.FrameModule
{
    public class GridConformSS : GH_Component
    {
        public GridConformSS()
            : base("Conform Surface-Surface", "ConformSS",
                "Generates a conforming lattice between two surfaces.",
                "IntraLattice2", "Frame")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("Surface 1", "S1", "First bounding surface", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface 2", "S2", "Second bounding surface", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip UV", "FlipUV", "Flip the UV parameters (for alignment purposes)", GH_ParamAccess.item, false); // default value is false
            pManager.AddIntegerParameter("Reverse UV", "RevUV", "0 : Keep as is\n1 : Reverse U-direction of s1\n2 : Reverse V-direction of s1\n", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Number u", "Nu", "Number of unit cells (u)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number v", "Nv", "Number of unit cells (v)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number w", "Nw", "Number of unit cells (w)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Morph", "Morph", "0: No Morph\n1: Space Morph\n2: Bezier Morph", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Morph Factor", "MF", "Contraction factor for bezier vectors (recommended: 2.0-3.0)", GH_ParamAccess.item, 3);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Strut curve network", GH_ParamAccess.list);
            pManager.AddPointParameter("Nodes", "Nodes", "Lattice Nodes", GH_ParamAccess.list);
            pManager.HideParameter(1); // Do not display the 'Nodes' output points
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate inputs
            var topology = new List<Line>();
            Surface s1 = null;
            Surface s2 = null;
            bool flipUV = false;
            int reverseUV = 0;
            int nU = 0;
            int nV = 0;
            int nW = 0;
            int morphed = 0;
            double morphFactor = 0;

            if (!DA.GetDataList(0, topology)) { return; }
            if (!DA.GetData(1, ref s1)) { return; }
            if (!DA.GetData(2, ref s2)) { return; }
            if (!DA.GetData(3, ref flipUV)) { return; }
            if (!DA.GetData(4, ref reverseUV)) { return; }
            if (!DA.GetData(5, ref nU)) { return; }
            if (!DA.GetData(6, ref nV)) { return; }
            if (!DA.GetData(7, ref nW)) { return; }
            if (!DA.GetData(8, ref morphed)) { return; }
            if (!DA.GetData(9, ref morphFactor)) { return; }

            if (topology.Count < 2) { return; }
            if (!s1.IsValid) { return; }
            if (!s2.IsValid) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // 2. Initialize the node tree, derivative tree and morphed space tree
            var nodeTree = new DataTree<Point3d>();                                 // will contain lattice nodes
            var derivTree = new DataTree<Vector3d>();                               // will contain derivatives (du,dv) in a parallel tree
            var spaceTree = new DataTree<GeometryBase>();                           // will contain the morphed uv spaces (as surface-surface, surface-axis or surface-point)

            // 3. Modify the UV parameters of surface1 if specified
            if (flipUV) s1 = s1.Transpose();
            if (reverseUV == 1 || reverseUV == 3) s1.Reverse(0, true);
            if (reverseUV == 2 || reverseUV == 3) s1.Reverse(1, true);           

            // 4. Package the number of cells in each direction into an array
            float[] N = new float[3] { nU, nV, nW };

            // 5. Normalize the UV-domain
            Interval normalDomain = new Interval(0,1);
            s1.SetDomain(0, normalDomain); // s1 u-direction
            s1.SetDomain(1, normalDomain); // s1 v-direction
            s2.SetDomain(0, normalDomain); // s2 u-direction
            s2.SetDomain(1, normalDomain); // s2 v-direction

            // 6. Prepare normalized/formatted unit cell topology
            var cell = new UnitCell();
            cell.ExtractTopology(topology); // fixes intersections, and formats lines to the UnitCell object
            cell.NormaliseTopology();       // normalizes the unit cell (scaled to unit size and moved to origin)
            cell.FormatTopology();          // sets up paths for inter-cell nodes

            // 7. Map nodes to design space
            //    Loop through the uvw cell grid
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // this loop maps each node in the cell onto the UV-surface maps
                        for (int i = 0; i < cell.Nodes.Count; i++)
                        {
                            // if the node belongs to another cell (i.e. it's relative path points outside the current cell)
                            if (cell.NodePaths[i][0] + cell.NodePaths[i][1] + cell.NodePaths[i][2] > 0)
                                continue;                            

                            Point3d pt1; Vector3d[] derivatives1; // initialize for surface 1
                            Point3d pt2; Vector3d[] derivatives2; // initialize for surface 2

                            double usub = cell.Nodes[i].X; // u-position within unit cell
                            double vsub = cell.Nodes[i].Y; // v-position within unit cell
                            double wsub = cell.Nodes[i].Z; // w-position within unit cell

                            GH_Path treePath = new GH_Path(u, v, w, i);    // u,v,w is the cell grid. the last index is for different nodes in each cell.

                            // these conditionals enforce the boundary, no nodes are created beyond the upper boundary
                            if (u == N[0] && usub != 0) continue;
                            if (v == N[1] && vsub != 0) continue;
                            if (w == N[2] && wsub != 0) continue;

                            // evaluate point and its derivatives on both surfaces
                            s1.Evaluate((u+usub) / N[0], (v+vsub) / N[1], 2, out pt1, out derivatives1);
                            s2.Evaluate((u+usub) / N[0], (v+vsub) / N[1], 2, out pt2, out derivatives2);

                            // create vector joining the two points (this is our w-range)
                            Vector3d wVect = pt2 - pt1;

                            // create the node, accounting for the position along the w-direction
                            Point3d newPt = pt1 + wVect * (w + wsub) / N[2];
                            nodeTree.Add(newPt, treePath);

                            // for each of the 2 directional directives (du and dv)
                            for (int derivIndex = 0; derivIndex < 2; derivIndex++)
                            {
                                // compute the interpolated derivative (need interpolation for in-between surfaces)
                                double interpolationFactor = (w + wsub) / N[2];
                                Vector3d deriv = derivatives1[derivIndex] + interpolationFactor * (derivatives2[derivIndex] - derivatives1[derivIndex]);
                                // this division scales the derivatives (gives better control of the bezier curves)
                                deriv = deriv / (morphFactor * N[derivIndex]);
                                derivTree.Add(deriv, treePath);
                            }

                        }
                    }

                    // Define the uv space map
                    if (u < N[0] && v < N[1])
                    {
                        GH_Path spacePath = new GH_Path(u, v);
                        var uInterval = new Interval((u) / N[0], (u + 1) / N[0]);                   // set trimming interval
                        var vInterval = new Interval((v) / N[1], (v + 1) / N[1]);
                        Surface ss1 = s1.Trim(uInterval, vInterval);                                // create sub-surface
                        Surface ss2 = s2.Trim(uInterval, vInterval);
                        ss1.SetDomain(0, normalDomain); ss1.SetDomain(1, normalDomain);     // normalize domain
                        ss2.SetDomain(0, normalDomain); ss2.SetDomain(1, normalDomain); 
                        // Save to the space tree
                        spaceTree.Add(ss1, spacePath);
                        spaceTree.Add(ss2, spacePath);
                    }
                    
                }
            }

            // 8. Generate the struts
            //     Simply loop through all unit cells, and enforce the cell topology (using cellStruts: pairs of node indices)
            var struts = new List<Curve>();
            var nodes = new Point3dList();
            struts = FrameTools.ConformMapping(nodeTree, derivTree, spaceTree, cell, N, morphed, morphFactor);
            struts = FrameTools.CleanNetwork(struts, out nodes);

            // 9. Set output
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
                //return Resources.circle5;
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
