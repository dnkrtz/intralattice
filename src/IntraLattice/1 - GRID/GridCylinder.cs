using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component generates a simple cylindrical lattice grid.
// ===========================================================
// Both the points and their interpolated derivatives are returned.

// Written by Aidan Kurtz (http://aidankurtz.com)

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
            pManager.AddNumberParameter("Number u", "Nu", "Number of unit cells (axial)", GH_ParamAccess.item, 15);
            pManager.AddNumberParameter("Number v", "Nv", "Number of unit cells (theta)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number w", "Nw", "Number of unit cells (radial)", GH_ParamAccess.item, 4);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Derivatives", "Derivs", "Directional derivatives", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate data
            double radius = 0;
            double height = 0;
            double nU = 0;
            double nV = 0;
            double nW = 0;

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

            // 2. Initialize the grid tree and derivatives tree
            var gridTree = new GH_Structure<GH_Point>();
            var derivTree = new GH_Structure<GH_Vector>();
            Plane basePlane = Plane.WorldXY;

            // 3. Define cylinder
            Surface cylinder = ( new Cylinder(new Circle(basePlane, radius), height) ).ToNurbsSurface();
            cylinder = cylinder.Transpose();    // transpose, the cylinder's uv isn't aligned with our convention

            // 4. Normalize the UV-domain
            cylinder.SetDomain(0, new Interval(0, 1)); // surface u-direction
            cylinder.SetDomain(1, new Interval(0, 1)); // surface v-direction

            // 5. Create grid of points (as data tree)
            //    u-direction is along the cylinder
            for (int u = 0; u <= nU; u++)
            {
                // v-direction travels around the cylinder axis (about axis)
                for (int v = 0; v <= nV; v++)
                {
                    Point3d pt1, pt2;
                    Vector3d[] derivatives;
                    // construct z-position vector
                    Vector3d vectorZ = height * basePlane.ZAxis * v / nV;
                    pt1 = basePlane.Origin + vectorZ;                                                   // compute pt1 (is on axis)
                    cylinder.Evaluate(u / nU, v / nV, 2, out pt2, out derivatives);     // compute pt2, and derivates (on surface)

                    // create vector joining these two points
                    Vector3d wVect = pt2 - pt1;

                    // create grid points on and between surface and axis
                    for (int w = 0; w <= nW; w++)
                    {
                        // aAdd point to gridTree
                        Point3d newPt = pt1 + wVect * w / nW;
                        GH_Path treePath = new GH_Path(u, v, w);
                        gridTree.Append(new GH_Point(newPt), treePath);

                        // add uv-derivatives to derivTree
                        derivTree.Append(new GH_Vector(derivatives[0] * w / nW), treePath);
                        derivTree.Append(new GH_Vector(derivatives[1] * w / nW), treePath);

                    }
                }
            }

            // 6. Set output
            DA.SetDataTree(0, gridTree);
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
