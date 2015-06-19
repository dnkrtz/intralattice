using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component generates a simple cylindrical lattice grid.

namespace IntraLattice
{
    public class GridCylinder : GH_Component
    {
        public GridCylinder()
            : base("GridCylinder", "GridCylinder",
                "Generates a lattice grid cylinder.",
                "IntraLattice2", "Grid")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Radius", "R", "Radius of cylinder", GH_ParamAccess.item, 15);
            pManager.AddNumberParameter("Height", "H", "Height of cylinder", GH_ParamAccess.item, 25);
            pManager.AddNumberParameter("Number u", "Nu", "Number of unit cells (axial)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number v", "Nv", "Number of unit cells (theta)", GH_ParamAccess.item, 15);
            pManager.AddNumberParameter("Number w", "Nw", "Number of unit cells (radial)", GH_ParamAccess.item, 4);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve and validate data
            double R = 0;
            double H = 0;
            double Nu = 0;
            double Nv = 0;
            double Nw = 0;

            if (!DA.GetData(0, ref R)) { return; }
            if (!DA.GetData(1, ref H)) { return; }
            if (!DA.GetData(2, ref Nu)) { return; }
            if (!DA.GetData(3, ref Nv)) { return; }
            if (!DA.GetData(4, ref Nw)) { return; }

            if (R == 0) { return; }
            if (H == 0) { return; }
            if (Nu == 0) { return; }
            if (Nv == 0) { return; }
            if (Nw == 0) { return; }

            // Declare gh_structure data tree
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();
            Point3d BasePoint = Plane.WorldXY.Origin;

            // Size of cells
            double Su = H / Nu;
            double Sv = 2 * Math.PI / Nv;
            double Sw = R / Nw;

            // Create grid of points (as data tree)
            // Axial loop (along axis)
            for (int i = 0; i <= Nu; i++)
            {
                // Theta loop (about axis)
                for (int j = 0; j <= Nv; j++)
                {
                    // Radial loop (away from axis)
                    for (int k = 0; k <= Nw; k++)
                    {
                        // Compute position vector (cartesian coordinates)
                        double Vx = (k * Sw) * (Math.Cos(j * Sv));
                        double Vy = (k * Sw) * (Math.Sin(j * Sv));
                        double Vz = i * Su;
                        Vector3d V = new Vector3d(Vx, Vy, Vz);

                        Point3d NewPt = BasePoint + V;

                        GH_Path TreePath = new GH_Path(0, i, j);           // Construct path in the tree
                        GridTree.Append(new GH_Point(NewPt), TreePath);    // Add point to GridTree
                    }
                }
            }

            // Set output
            DA.SetDataTree(0, GridTree);
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
