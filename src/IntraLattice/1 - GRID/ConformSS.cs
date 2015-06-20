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
    public class ConformSS : GH_Component
    {
        public ConformSS()
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
            Surface S1 = null;
            Surface S2 = null;
            double Nu = 0;
            double Nv = 0;
            double Nw = 0;

            // Attempt to fetch data
            if (!DA.GetData(0, ref S1)) { return; }
            if (!DA.GetData(1, ref S2)) { return; }
            if (!DA.GetData(2, ref Nu)) { return; }
            if (!DA.GetData(3, ref Nv)) { return; }
            if (!DA.GetData(4, ref Nw)) { return; }

            // Validate data
            if (!S1.IsValid) { return; }
            if (!S2.IsValid) { return; }
            if (Nu == 0) { return; }
            if (Nv == 0) { return; }
            if (Nw == 0) { return; }

            // Initialize the grid of points
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();
            Vector3d[] derivatives; // not used, but needed for Evaluate method

            // i, j loops over UV
            for (int i = 0; i <= Nu; i++)
            {
                for (int j = 0; j <= Nv; j++)
                {
                    // Find the pair of points on both surfaces
                    // On surface 1
                    Point3d Pt1;
                    double Uparam = S1.Domain(0).T0 + (i / Nu) * S1.Domain(0).Length;
                    double Vparam = S1.Domain(1).T0 + (j / Nv) * S1.Domain(1).Length;
                    S1.Evaluate(Uparam, Vparam, 0, out Pt1, out derivatives);  // Evaluate point
                    // On surface 2
                    Point3d Pt2;
                    Uparam = S2.Domain(0).T0 + (i / Nu) * S2.Domain(0).Length;
                    Vparam = S2.Domain(1).T0 + (j / Nv) * S2.Domain(1).Length;
                    S2.Evaluate(Uparam, Vparam, 0, out Pt2, out derivatives);   // Evaluate point

                    // Create vector joining these two points
                    Vector3d wVect = Pt2 - Pt1;

                    // Create grid points on and between surfaces
                    for (int k = 0; k <= Nw; k++)
                    {
                        Point3d NewPt = Pt1 + wVect * k / Nw;

                        GH_Path TreePath = new GH_Path(i, j, k);
                        GridTree.Append(new GH_Point(NewPt), TreePath);
                    }
                }
            }

            // Output grid
            DA.SetDataTree(0, GridTree);

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
