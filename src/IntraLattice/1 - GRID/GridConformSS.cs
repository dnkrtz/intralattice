using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component generates a conformal lattice grid between two surfaces.
// Assumption : The surfaces are oriented in the same direction (for UV-Map indices)

// Written by Aidan Kurtz

namespace IntraLattice
{
    public class GridConformSS : GH_Component
    {
        public GridConformSS()
            : base("Conform Surface-Surface", "ConformSS",
                "Generates a conforming point grid between two surfaces.",
                "IntraLattice2", "Grid")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface 1", "S1", "First bounding surface", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface 2", "S2", "Second bounding surface", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Number u", "Nu", "Number of unit cells (u)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number v", "Nv", "Number of unit cells (v)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number w", "Nw", "Number of unit cells (w)", GH_ParamAccess.item, 5);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Derivatives", "Derivs", "Directional derivatives", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables and assign initial invalid data.
            Surface s1 = null;
            Surface s2 = null;
            int nU = 0;
            int nV = 0;
            int nW = 0;

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
            GH_Structure<GH_Point> gridTree = new GH_Structure<GH_Point>();     // will contain point grid
            GH_Structure<GH_Vector> derivTree = new GH_Structure<GH_Vector>();   // will contain derivatives (du,dv) in a parallel tree

            List<int> N = new List<int> { nU, nV, nW };

            // Normalize the UV-domain
            Interval normalizedDomain = new Interval(0,1);
            s1.SetDomain(0, normalizedDomain); // s1 u-direction
            s1.SetDomain(1, normalizedDomain); // s1 v-direction
            s2.SetDomain(0, normalizedDomain); // s2 u-direction
            s2.SetDomain(1, normalizedDomain); // s2 v-direction

            // Let's create the actual point grid now
            // i, j loops over UV
            for (int i = 0; i <= N[0]; i++)
            {
                for (int j = 0; j <= N[1]; j++)
                {
                    Point3d pt1; // On surface 1
                    Point3d pt2; // On surface 2
                    Vector3d[] derivatives1;
                    Vector3d[] derivatives2;
                    // Compute uv parameters
                    double uParam = (i / (double)nU);
                    double vParam = (j / (double)nV);
                    // Evaluate point locations and derivatives
                    s1.Evaluate(uParam, vParam, 2, out pt1, out derivatives1);
                    s2.Evaluate(uParam, vParam, 2, out pt2, out derivatives2);

                    // Create vector joining the two points, this is our w-direction
                    Vector3d wVect = pt2 - pt1;

                    // Create grid points on and between surfaces
                    for (int k = 0; k <= N[2]; k++)
                    {
                        GH_Path treePath = new GH_Path(i, j, k);    // path in the trees

                        // save point to gridTree
                        Point3d newPt = pt1 + wVect * k / nW;
                        gridTree.Append(new GH_Point(newPt), treePath);
                        
                        // for each of the 2 directional directives
                        for (int derivIndex = 0; derivIndex < 2; derivIndex++ )
                        {
                            // compute the interpolated derivative (need interpolation for in-between surfaces)
                            double interpolationFactor = k/nW;
                            Vector3d deriv = derivatives1[derivIndex] + interpolationFactor*(derivatives2[derivIndex]-derivatives1[derivIndex]);
                            derivTree.Append(new GH_Vector(deriv), treePath);
                        }
                    }
                }
            }

            // Output grid
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
            get { return new Guid("{ac0814b4-00e7-4efb-add5-e845a831c6da}"); }
        }
    }
}
