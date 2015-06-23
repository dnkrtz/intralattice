using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Intersect;

// This component generates a conformal lattice grid between a surface and an axis
// ===============================================================================
// The axis can be an open curve or a closed curve. Of course, it may also be a straight line.
// The surface does not need to loop a full 360 degrees around the axis.
// Our implementation assumes that the axis is a set of U parameters, thus it should be aligned with U parameters of the surface.
// The flipUV input allows the user to swap U and V parameters of the surface.

// Written by Aidan Kurtz (http://aidankurtz.com)

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
            pManager.AddSurfaceParameter("Surface", "Surface", "Surface to conform to", GH_ParamAccess.item);
            pManager.AddCurveParameter("Axis", "A", "Axis (may be curved)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip UV", "FlipUV", "Flip the U and V parameters on the surface", GH_ParamAccess.item, false); // default value is true
            pManager.AddNumberParameter("Number u", "Nu", "Number of unit cells (u)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number v", "Nv", "Number of unit cells (v)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number w", "Nw", "Number of unit cells (w)", GH_ParamAccess.item, 5);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "Grid", "Point grid", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Derivatives", "Derivs", "Directional derivatives", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables
            Surface surface = null;
            Curve axis = null;
            bool flipUV = false;
            double nU = 0;
            double nV = 0;
            double nW = 0;

            // 2.   Attempt to fetch data
            if (!DA.GetData(0, ref surface)) { return; }
            if (!DA.GetData(1, ref axis)) { return; }
            if (!DA.GetData(2, ref flipUV)) { return; }
            if (!DA.GetData(3, ref nU)) { return; }
            if (!DA.GetData(4, ref nV)) { return; }
            if (!DA.GetData(5, ref nW)) { return; }

            // 3. Validate data, if invalid, abort
            if (!surface.IsValid) { return; }
            if (!axis.IsValid) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // 4. Initialize the grid tree and derivatives tree
            var gridTree = new GH_Structure<GH_Point>();
            var derivTree = new GH_Structure<GH_Vector>();

            // 5. Flip the UV parameters if specified
            if (flipUV) surface = surface.Transpose();

            // 5. Normalize the UV-domain
            Interval normalizedDomain = new Interval(0, 1);
            surface.SetDomain(0, normalizedDomain); // surface u-direction
            surface.SetDomain(1, normalizedDomain); // surface v-direction
            axis.Domain = normalizedDomain; // axis (u-direction)

            // 6. Divide axis into equal segments, get curve parameters
            double[] curveParams = axis.DivideByCount((int)nU, true);
            //    If axis is closed curve, add last parameter to close the loop
            if (axis.IsClosed) curveParams[curveParams.Length] = curveParams[0]; 

            // 7. Let's create the actual point grid now
            for (int u = 0; u <= nU; u++)
            {
                for (int v = 0; v <= nV; v++)
                {
                    // Evaluate the point on the axis
                    Point3d pt1 = axis.PointAt(curveParams[u]);

                    // Evaluate the point and its derivatives at the current uv parameters
                    Point3d pt2;
                    Vector3d[] derivatives;
                    surface.Evaluate(u/nU, v/nV, 2, out pt2, out derivatives);

                    // Create vector joining these two points
                    Vector3d wVect = pt2 - pt1;

                    // Create grid points on and between surface and axis
                    for (int w = 0; w <= nW; w++)
                    {
                        // Add point to gridTree
                        Point3d newPt = pt1 + wVect * w / nW;
                        GH_Path treePath = new GH_Path(u, v, w);        // construct path in the trees
                        gridTree.Append(new GH_Point(newPt), treePath);

                        // Add uv-derivatives to derivTree
                        // Decrease the amplitude of the derivative vector as we approach the axis
                        derivTree.Append(new GH_Vector(derivatives[0] * w / nW), treePath);
                        derivTree.Append(new GH_Vector(derivatives[1] * w / nW), treePath);
                    }
                }
            }

            // 8. Set output
            DA.SetDataTree(0, gridTree);
            DA.SetDataTree(1, derivTree);

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
