using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component generates a simple spherical lattice grid.

namespace IntraLattice
{
    public class GridSphere : GH_Component
    {
        public GridSphere()
            : base("GridSphere", "GridSphere",
                "Generates a lattice grid sphere.",
                "IntraLattice2", "Grid")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Radius", "R", "Radius of cylinder", GH_ParamAccess.item, 15);
            pManager.AddIntegerParameter("Number u", "Nu", "Number of unit cells (theta)", GH_ParamAccess.item, 10);
            pManager.AddIntegerParameter("Number v", "Nv", "Number of unit cells (phi)", GH_ParamAccess.item, 10);
            pManager.AddIntegerParameter("Number w", "Nw", "Number of unit cells (radial)", GH_ParamAccess.item, 3);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve and validate data
            double R = 0;
            int Nu = 0;
            int Nv = 0;
            int Nw = 0;

            if (!DA.GetData(0, ref R)) { return; }
            if (!DA.GetData(1, ref Nu)) { return; }
            if (!DA.GetData(2, ref Nv)) { return; }
            if (!DA.GetData(3, ref Nw)) { return; }

            if (R == 0) { return; }
            if (Nu == 0) { return; }
            if (Nv == 0) { return; }
            if (Nw == 0) { return; }

            // Declare gh_structure data tree
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();
            Point3d BasePoint = Plane.WorldXY.Origin;

            // Size of cells
            double Su = Math.PI / Nu;
            double Sv = 2 * Math.PI / Nv;
            double Sw = R / Nw;

            // Create grid of points (as data tree)
            // Theta loop (polar)
            for (int i = 0; i <= Nu; i++)
            {
                // Phi loop (azimuthal)
                for (int j = 0; j <= Nv; j++)
                {
                    // Radial loop (away from center)
                    for (int k = 0; k <= Nw; k++)
                    {
                        // Compute position vector (cartesian coordinates)
                        double Vx = (k * Sw) * (Math.Sin(i * Su)) * (Math.Cos(j * Sv));
                        double Vy = (k * Sw) * (Math.Sin(i * Su)) * (Math.Sin(j * Sv));
                        double Vz = (k * Sw) * (Math.Cos(i * Su));
                        Vector3d V = new Vector3d(Vx, Vy, Vz);

                        // Create new point
                        Point3d NewPt = BasePoint + V;

                        GH_Path TreePath = new GH_Path(i, j, k);           // Construct path in the tree
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
            get { return new Guid("{c390a2fe-3307-4082-92d1-78603d15681a}"); }
        }
    }
}
