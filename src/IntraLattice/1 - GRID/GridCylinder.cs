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
            pManager.AddIntegerParameter("Number u", "Nu", "Number of unit cells (axial)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number v", "Nv", "Number of unit cells (theta)", GH_ParamAccess.item, 15);
            pManager.AddIntegerParameter("Number w", "Nw", "Number of unit cells (radial)", GH_ParamAccess.item, 4);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve and validate data
            double radius = 0;
            double height = 0;
            int nU = 0;
            int nV = 0;
            int nW = 0;

            if (!DA.GetData(0, ref radius)) { return; }
            if (!DA.GetData(1, ref height)) { return; }
            if (!DA.GetData(2, ref nU)) { return; }
            if (!DA.GetData(3, ref nV)) { return; }
            if (!DA.GetData(4, ref nW)) { return; }

            if (radius == 0) { return; }
            if (height == 0) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // Declare gh_structure data tree
            GH_Structure<GH_Point> gridTree = new GH_Structure<GH_Point>();
            Point3d basePoint = Plane.WorldXY.Origin;

            // Size of cells
            double uCellSize = height / nU;
            double vCellSize = 2 * Math.PI / nV;
            double wCellSize = radius / nW;

            // Create grid of points (as data tree)
            // Axial loop (along axis)
            for (int i = 0; i <= nU; i++)
            {
                // Theta loop (about axis)
                for (int j = 0; j <= nV; j++)
                {
                    // Radial loop (away from axis)
                    for (int k = 0; k <= nW; k++)
                    {
                        // Compute position vector (cartesian coordinates)
                        double vectorU = (k * wCellSize) * (Math.Cos(j * vCellSize));
                        double vectorV = (k * wCellSize) * (Math.Sin(j * vCellSize));
                        double vectorW = i * uCellSize;
                        Vector3d V = new Vector3d(vectorU, vectorV, vectorW);

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
            get { return new Guid("{9f6769c0-dec5-4a0d-8ade-76fca1dfd4e3}"); }
        }
    }
}
