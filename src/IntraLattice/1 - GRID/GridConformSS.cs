using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component generates a conformal lattice grid between two surfaces.
// Assumption : The surfaces are oriented in the same direction (for UV-Map indices)

namespace IntraLattice
{
    public class GridConformSS : GH_Component
    {
        public GridConformSS()
            : base("ConformSS", "ConfSS",
                "Generates a conforming point grid between two surfaces.",
                "IntraLattice2", "Grid")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface 1", "S1", "First bounding surface", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface 2", "S2", "Second bounding surface", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number u", "Nu", "Number of unit cells (u)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number v", "Nv", "Number of unit cells (v)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number w", "Nw", "Number of unit cells (w)", GH_ParamAccess.item, 5);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables and assign initial invalid data.
            Surface s1 = null;
            Surface s2 = null;
            double nU = 0;
            double nV = 0;
            double nW = 0;

            // Attempt to fetch data
            if (!DA.GetData(0, ref s1)) { return; }
            if (!DA.GetData(1, ref s2)) { return; }
            if (!DA.GetData(2, ref nU)) { return; }
            if (!DA.GetData(3, ref nV)) { return; }
            if (!DA.GetData(4, ref nW)) { return; }

            // Validate data
            if (!s1.IsValid) { return; }
            if (!s2.IsValid) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // Initialize the grid of points
            GH_Structure<GH_Point> gridTree = new GH_Structure<GH_Point>();
            Vector3d[] derivatives; // not used, but needed for Evaluate method

            // i, j loops over UV
            for (int i = 0; i <= nU; i++)
            {
                for (int j = 0; j <= nV; j++)
                {
                    // Find the pair of points on both surfaces
                    // On surface 1
                    Point3d pt1;
                    double uParam = s1.Domain(0).T0 + (i / nU) * s1.Domain(0).Length;
                    double vParam = s1.Domain(1).T0 + (j / nV) * s1.Domain(1).Length;
                    s1.Evaluate(uParam, vParam, 0, out pt1, out derivatives);  // Evaluate point
                    // On surface 2
                    Point3d pt2;
                    uParam = s2.Domain(0).T0 + (i / nU) * s2.Domain(0).Length;
                    vParam = s2.Domain(1).T0 + (j / nV) * s2.Domain(1).Length;
                    s2.Evaluate(uParam, vParam, 0, out pt2, out derivatives);   // Evaluate point

                    // Create vector joining these two points
                    Vector3d wVect = pt2 - pt1;

                    // Create grid points on and between surfaces
                    for (int k = 0; k <= nW; k++)
                    {
                        Point3d newPt = pt1 + wVect * k / nW;

                        GH_Path treePath = new GH_Path(i, j, k);
                        gridTree.Append(new GH_Point(newPt), treePath);
                    }
                }
            }

            // Output grid
            DA.SetDataTree(0, gridTree);

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
