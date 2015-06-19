using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Intersect;

// This component generates a conformal lattice grid between a surface and an axis
// TWO METHODS
// 1. Based on UV-Map of surface (UV = True)
// 2. Based on custom map that follows axis direction (UV = False)
// Assumption : The surface rotates the full 360degrees around the axis (for second method).

namespace IntraLattice
{
    public class ConformSA : GH_Component
    {
        public ConformSA()
            : base("ConformSA", "ConfSA",
                "Generates conforming lattice grid between a surface and an axis",
                "IntraLattice2", "Grid")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Surface to conform to", GH_ParamAccess.item);
            pManager.AddCurveParameter("Axis", "A", "Axis (may be curved)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("UV Mapping", "UV", "True = Use UV-Map\nFalse = Use Cylindrical-Map", GH_ParamAccess.item, true); // default value is true
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
            Surface S = null;
            Curve A = null;
            Boolean UV = new Boolean();
            double Nu = 0;
            double Nv = 0;
            double Nw = 0;

            // Attempt to fetch data
            if (!DA.GetData(0, ref S)) { return; }
            if (!DA.GetData(1, ref A)) { return; }
            if (!DA.GetData(2, ref UV)) { return; }
            if (!DA.GetData(3, ref Nu)) { return; }
            if (!DA.GetData(4, ref Nv)) { return; }
            if (!DA.GetData(5, ref Nw)) { return; }

            // Validate data
            if (!S.IsValid) { return; }
            if (!A.IsValid) { return; }
            if (Nu == 0) { return; }
            if (Nv == 0) { return; }
            if (Nw == 0) { return; }

            // Initialize the grid of points
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();


            // Use UV-Map Method (note, should add check to make sure axis is aligned with u)
            if (UV)
            {
                Vector3d[] derivatives; // not used, but needed for Evaluate method
                List<double> Params = new List<double>(A.DivideByCount((int)Nu, true));

                if (A.IsClosed) Params.Add(Params[0]);  // if axis is closed curve, add last parameter to close the loop

                // i, j loops over UV
                for (int i = 0; i <= Nu; i++)
                {
                    double Param = Params[i];
                    for (int j = 0; j <= Nv; j++)
                    {
                        // Find the pair of points on surface and axis
                        Point3d Pt1 = A.PointAt(Param);
                        Point3d Pt2;
                        S.Evaluate((i / Nw) * S.Domain(0).Length, (j / Nv) * S.Domain(1).Length, 0, out Pt2, out derivatives);

                        // Create vector joining these two points
                        Vector3d wVect = Pt2 - Pt1;

                        // Create grid points on and between surfaces
                        for (int k = 0; k <= Nw; k++)
                        {
                            Point3d NewPt = Pt1 + wVect * k / Nu;
                            GH_Path TreePath = new GH_Path(0, i, j);
                            GridTree.Append(new GH_Point(NewPt), TreePath);
                        }
                    }
                }
            }
            // Here we don't use UV-Map of surface. Rather, we use planes perpendicular to the axis to intersect the surface.
            // Kindof like drawing our own UV-Map. In some cases the two methods give the same result, but not necessarily.
            else
            {
                // Prepare divisions along axis ('uNum' divisions)
                List<double> Params = new List<double>(A.DivideByCount((int)Nu, true)); // divide curve into zNum divisions
                Plane[] BasePlanes = A.GetPerpendicularFrames(Params);  // get perpendicular planes at each division point

                // For now, assuming surface covers full 360degree rotation
                List<double> Angles = new List<double>();
                for (int i = 0; i < Nv; i++) Angles.Add(2 * Math.PI * i / Nv);

                // Loop along axis
                for (int i = 0; i < BasePlanes.Length; i++)
                {
                    Plane BasePlane = BasePlanes[i];
                    // Loop about axis
                    for (int j = 0; j < Angles.Count; j++)
                    {
                        double Angle = Angles[j];
                        Vector3d RVect = BasePlane.PointAt(Math.Cos(Angle), Math.Sin(Angle)) - BasePlane.Origin; // Radial unit vector
                        Ray3d RRay = new Ray3d(BasePlane.Origin, RVect);
                        Point3d SurfPt = Intersection.RayShoot(RRay, new List<Surface> { S }, 1)[0];   // Shoot ray to intersect surface
                        RVect = SurfPt - BasePlane.Origin;  // Update radial vector (changes amplitude, direction unchanged)

                        // Loop away from axis
                        for (int k = 0; k <= Nw; k++)
                        {
                            Point3d NewPt = BasePlane.Origin + RVect * k / Nw;

                            GH_Path TreePath = new GH_Path(0, i, j);           // Construct path in the tree
                            GridTree.Append(new GH_Point(NewPt), TreePath);    // Add point to GridTree
                        }
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
            get { return new Guid("{e0e8a858-66bd-4145-b173-23dc2e247206}"); }
        }
    }
}
