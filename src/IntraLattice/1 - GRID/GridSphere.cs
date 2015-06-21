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
            double radius= 0;
            int nU = 0;
            int nV = 0;
            int nW = 0;

            if (!DA.GetData(0, ref radius)) { return; }
            if (!DA.GetData(1, ref nU)) { return; }
            if (!DA.GetData(2, ref nV)) { return; }
            if (!DA.GetData(3, ref nW)) { return; }

            if (radius == 0) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // Declare gh_structure data tree
            GH_Structure<GH_Point> gridTree = new GH_Structure<GH_Point>();
            Point3d basePoint = Plane.WorldXY.Origin;

            // Size of cells
            double uCellSize = Math.PI / nU;
            double vCellSize = 2 * Math.PI / nV;
            double wCellSize = radius / nW;

            // Create grid of points (as data tree)
            // Theta loop (polar)
            for (int i = 0; i <= nU; i++)
            {
                // Phi loop (azimuthal)
                for (int j = 0; j <= nV; j++)
                {
                    // Radial loop (away from center)
                    for (int k = 0; k <= nW; k++)
                    {
                        // Compute position vector (cartesian coordinates)
                        double vectorX = (k * wCellSize) * (Math.Sin(i * uCellSize)) * (Math.Cos(j * vCellSize));
                        double vectorY = (k * wCellSize) * (Math.Sin(i * uCellSize)) * (Math.Sin(j * vCellSize));
                        double vectorZ = (k * wCellSize) * (Math.Cos(i * uCellSize));
                        Vector3d V = new Vector3d(vectorX, vectorY, vectorZ);

                        // Create new point
                        Point3d newPt = basePoint + V;

                        GH_Path treePath = new GH_Path(i, j, k);           // Construct path in the tree
                        gridTree.Append(new GH_Point(newPt), treePath);    // Add point to GridTree
                    }
                }
            }

            // Set output
            DA.SetDataTree(0, gridTree);
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
