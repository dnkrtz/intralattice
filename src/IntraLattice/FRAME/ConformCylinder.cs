using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino;

// This component generates a simple cylindrical lattice grid.
// ===========================================================
// Both the points and their interpolated derivatives are returned.

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class ConformCylinder : GH_Component
    {
        public ConformCylinder()
            : base("ConformCylinder", "ConformCylinder",
                "Generates a conformal lattice cylinder.",
                "IntraLattice2", "Frame")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius", "R", "Radius of cylinder", GH_ParamAccess.item, 15);
            pManager.AddNumberParameter("Height", "H", "Height of cylinder", GH_ParamAccess.item, 25);
            pManager.AddNumberParameter("Number u", "Nu", "Number of unit cells (axial)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number v", "Nv", "Number of unit cells (theta)", GH_ParamAccess.item, 15);
            pManager.AddNumberParameter("Number w", "Nw", "Number of unit cells (radial)", GH_ParamAccess.item, 4);
            pManager.AddBooleanParameter("Morph", "Morph", "If true, struts will morph to the design space (as bezier curves)", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Morph Factor", "MF", "Division factor for bezier vectors (recommended: 2.0-3.0)", GH_ParamAccess.item, 3);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Derivatives", "Derivs", "Directional derivatives", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate data
            var topology = new List<Curve>();
            double radius = 0;
            double height = 0;
            double nU = 0;
            double nV = 0;
            double nW = 0;
            bool morphed = false;
            double morphFactor = 0;

            if (!DA.GetDataList(0, topology)) { return; }
            if (!DA.GetData(1, ref radius)) { return; }
            if (!DA.GetData(2, ref height)) { return; }
            if (!DA.GetData(3, ref nU)) { return; }
            if (!DA.GetData(4, ref nV)) { return; }
            if (!DA.GetData(5, ref nW)) { return; }
            if (!DA.GetData(6, ref morphed)) { return; }
            if (!DA.GetData(7, ref morphFactor)) { return; }

            if (topology.Count < 2) { return; }
            if (radius == 0) { return; }
            if (height == 0) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // 2. Initialize the grid tree and derivatives tree
            var nodeTree = new GH_Structure<GH_Point>();
            var derivTree = new GH_Structure<GH_Vector>();
            
            // 3. Define cylinder
            Plane basePlane = Plane.WorldXY;
            Surface cylinder = ( new Cylinder(new Circle(basePlane, radius), height) ).ToNurbsSurface();
            cylinder = cylinder.Transpose();

            // 4. Package the number of cells in each direction into an array
            double[] N = new double[3] { nU, nV, nW };

            // 5. Normalize the UV-domain
            cylinder.SetDomain(0, new Interval(0, 1)); // surface u-direction
            cylinder.SetDomain(1, new Interval(0, 1)); // surface v-direction

            // 6. Prepare normalized unit cell topology
            var cellNodes = new Point3dList();
            var cellStruts = new List<IndexPair>();
            TopologyTools.Topologize(ref topology, ref cellNodes, ref cellStruts);  // converts list of lines into an adjacency list format (cellNodes and cellStruts)
            TopologyTools.NormaliseTopology(ref cellNodes); // normalizes the unit cell (scaled to unit size and moved to origin)

            // 7. Create grid of points (as data tree)
            //    u-direction is along the cylinder
            for (int u = 0; u <= N[0]; u++)
            {
                // v-direction travels around the cylinder axis (about axis)
                for (int v = 0; v <= N[1]; v++)
                {
                    // this loop maps each node in the cell onto the UV-surface maps
                    for (int nodeIndex = 0; nodeIndex < cellNodes.Count; nodeIndex++)
                    {
                        Point3d pt1, pt2;
                        Vector3d[] derivatives;

                        double usub = cellNodes[nodeIndex].X; // u-position within unit cell
                        double vsub = cellNodes[nodeIndex].Y; // v-position within unit cell
                        double wsub = cellNodes[nodeIndex].Z; // w-position within unit cell

                        // construct z-position vector
                        Vector3d vectorZ = height * basePlane.ZAxis * (u+usub) / N[0];
                        pt1 = basePlane.Origin + vectorZ;                                   // compute pt1 (is on axis)
                        cylinder.Evaluate( (u+usub)/N[0], (v+vsub)/N[1], 2, out pt2, out derivatives);     // compute pt2, and derivates (on surface)

                        // create vector joining these two points
                        Vector3d wVect = pt2 - pt1;

                        // create grid points on and between surface and axis
                        for (int w = 0; w <= N[2]; w++)
                        {
                            // these conditionals enforce the boundary, no nodes are created beyond the upper boundary
                            if (u == N[0] && usub != 0) continue;
                            if (v == N[1] && vsub != 0) continue;
                            if (w == N[2] && wsub != 0) continue;

                            // add point to gridTree
                            Point3d newPt = pt1 + wVect * (w+wsub)/N[2];
                            GH_Path treePath = new GH_Path(u, v, w, nodeIndex);
                            nodeTree.Append(new GH_Point(newPt), treePath);

                            // for each of the 2 directional directives (du and dv)
                            for (int derivIndex = 0; derivIndex < 2; derivIndex++)
                            {
                                // decrease the amplitude of the derivative vector as we approach the axis
                                Vector3d deriv = derivatives[derivIndex] * (w + wsub) / N[2];
                                // this division scales the derivatives (gives better control of the bezier curves)
                                deriv = deriv / (morphFactor * N[derivIndex]);
                                derivTree.Append(new GH_Vector(deriv), treePath);
                            }
                        }
                    }
                }
            }

            // 7. Generate the struts
            //     Simply loop through all unit cells, and enforce the cell topology (using cellStruts: pairs of node indices)
            var struts = new List<GH_Curve>();
            TopologyTools.ConformMapping(ref struts, ref nodeTree, ref derivTree, cellStruts, cellNodes, N, morphed);

            // 8. Set output
            DA.SetDataTree(0, nodeTree);
            DA.SetDataTree(1, derivTree);
        }

        // Primitive grid component -> first panel of the toolbar
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
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

        public override Guid ComponentGuid
        {
            get { return new Guid("{9f6769c0-dec5-4a0d-8ade-76fca1dfd4e3}"); }
        }
    }
}
