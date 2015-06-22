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
    public class GridConformSA : GH_Component
    {
        public GridConformSA()
            : base("Conform Surface-Axis", "ConformSA",
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
            Surface surface = null;
            Curve axis = null;
            Boolean withUV = new Boolean();
            double nU = 0;
            double nV = 0;
            double nW = 0;

            // Attempt to fetch data
            if (!DA.GetData(0, ref surface)) { return; }
            if (!DA.GetData(1, ref axis)) { return; }
            if (!DA.GetData(2, ref withUV)) { return; }
            if (!DA.GetData(3, ref nU)) { return; }
            if (!DA.GetData(4, ref nV)) { return; }
            if (!DA.GetData(5, ref nW)) { return; }

            // Validate data
            if (!surface.IsValid) { return; }
            if (!axis.IsValid) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // Initialize the grid of points
            GH_Structure<GH_Point> gridTree = new GH_Structure<GH_Point>();


            // Use UV-Map Method (note, should add check to make sure axis is aligned with u, as opposed to v)
            if (withUV)
            {
                Vector3d[] derivatives; // not used, but needed for Evaluate method
                List<double> curveParams = new List<double>(axis.DivideByCount((int)nU, true)); // divide curve into equal segments, get curve parameters

                if (axis.IsClosed) curveParams.Add(curveParams[0]);  // if axis is closed curve, add last parameter to close the loop

                // i, j loops over UV
                for (int i = 0; i <= nU; i++)
                {
                    double curveParam = curveParams[i];
                    for (int j = 0; j <= nV; j++)
                    {
                        // Find the pair of points on surface and axis
                        Point3d pt1 = axis.PointAt(curveParam);
                        Point3d pt2;
                        double uParam = (i/nU) * surface.Domain(0).Length;
                        double vParam = (j/nV) * surface.Domain(1).Length;
                        surface.Evaluate(uParam, vParam, 0, out pt2, out derivatives);

                        // Create vector joining these two points
                        Vector3d wVect = pt2 - pt1;

                        // Create grid points on and between surface and axis
                        for (int k = 0; k <= nW; k++)
                        {
                            Point3d newPt = pt1 + wVect * k / nW;
                            GH_Path treePath = new GH_Path(i, j, k);
                            gridTree.Append(new GH_Point(newPt), treePath);
                        }
                    }
                }
            }
            // Here we don't use UV-Map of surface. Rather, we use planes perpendicular to the axis to intersect the surface.
            // Kindof like drawing our own UV-Map. In some cases the two methods give the same result, but not necessarily.
            else
            {
                // Prepare divisions along axis ('uNum' divisions)
                List<double> curveParams = new List<double>(axis.DivideByCount((int)nU, true)); // divide curve into zNum divisions
                Plane[] basePlanes = axis.GetPerpendicularFrames(curveParams);  // get perpendicular planes at each division point

                // For now, assuming surface covers full 360degree rotation
                List<double> angles = new List<double>();
                for (int i = 0; i < nV; i++) angles.Add(2 * Math.PI * i / nV);

                // Loop along axis
                for (int i = 0; i < basePlanes.Length; i++)
                {
                    Plane basePlane = basePlanes[i];
                    // Loop about axis
                    for (int j = 0; j < angles.Count; j++)
                    {
                        double angle = angles[j];
                        Vector3d rVect = basePlane.PointAt(Math.Cos(angle), Math.Sin(angle)) - basePlane.Origin; // Radial unit vector
                        Ray3d rRay = new Ray3d(basePlane.Origin, rVect);
                        Point3d surfPt = Intersection.RayShoot(rRay, new List<Surface> { surface }, 1)[0];   // Shoot ray to intersect surface
                        rVect = surfPt - basePlane.Origin;  // Update radial vector (changes amplitude, direction unchanged)

                        // Loop away from axis
                        for (int k = 0; k <= nW; k++)
                        {
                            Point3d newPt = basePlane.Origin + rVect * k / nW;

                            GH_Path treePath = new GH_Path(0, i, j);           // Construct path in the tree
                            gridTree.Append(new GH_Point(newPt), treePath);    // Add point to GridTree
                        }
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
            get { return new Guid("{e0e8a858-66bd-4145-b173-23dc2e247206}"); }
        }
    }
}
