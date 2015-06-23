using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component generates a conformal lattice grid between two surfaces.
// =======================================================================
// Assumption : The surfaces are oriented in the same direction (for UV-Map indices)

// Written by Aidan Kurtz (http://aidankurtz.com)

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
            pManager.AddBooleanParameter("Flip UV", "FlipUV", "Flip the UV parameters (for alignment purposes)", GH_ParamAccess.item, false); // default value is false
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
            Surface s1 = null;
            Surface s2 = null;
            bool flipUV = false;
            double nU = 0;
            double nV = 0;
            double nW = 0;

            // 2. Attempt to fetch data
            if (!DA.GetData(0, ref s1)) { return; }
            if (!DA.GetData(1, ref s2)) { return; }
            if (!DA.GetData(2, ref flipUV)) { return; }
            if (!DA.GetData(3, ref nU)) { return; }
            if (!DA.GetData(4, ref nV)) { return; }
            if (!DA.GetData(5, ref nW)) { return; }

            // 3. Validate data
            if (!s1.IsValid) { return; }
            if (!s2.IsValid) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // 4. Initialize the grid tree and derivatives tree
            var gridTree = new GH_Structure<GH_Point>();     // will contain point grid
            var derivTree = new GH_Structure<GH_Vector>();   // will contain derivatives (du,dv) in a parallel tree

            // 5. Flip the UV parameters a surface if specified
            if (flipUV) s1 = s1.Transpose();
            
            // 6. Package the number of increments in each direction as a list
            var N = new List<double> { nU, nV, nW };

            // 7. Normalize the UV-domain
            Interval normalizedDomain = new Interval(0,1);
            s1.SetDomain(0, normalizedDomain); // s1 u-direction
            s1.SetDomain(1, normalizedDomain); // s1 v-direction
            s2.SetDomain(0, normalizedDomain); // s2 u-direction
            s2.SetDomain(1, normalizedDomain); // s2 v-direction

            // 8. Let's create the actual point grid now
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    Point3d pt1; // On surface 1
                    Vector3d[] derivatives1;
                    Point3d pt2; // On surface 2
                    Vector3d[] derivatives2;
                    
                    // evaluate point and its derivatives on both surface
                    s1.Evaluate(u/nU, v/nV, 2, out pt1, out derivatives1);
                    s2.Evaluate(u/nU, v/nV, 2, out pt2, out derivatives2);

                    // create vector joining the two points
                    Vector3d wVect = pt2 - pt1;

                    // create grid points on and between surfaces
                    for (int w = 0; w <= N[2]; w++)
                    {
                        GH_Path treePath = new GH_Path(u, v, w);    // path in the trees

                        // save point to gridTree
                        Point3d newPt = pt1 + wVect * w / nW;
                        gridTree.Append(new GH_Point(newPt), treePath);
                        
                        // for each of the 2 directional directives
                        for (int derivIndex = 0; derivIndex < 2; derivIndex++ )
                        {
                            // compute the interpolated derivative (need interpolation for in-between surfaces)
                            double interpolationFactor = w/nW;
                            Vector3d deriv = derivatives1[derivIndex] + interpolationFactor*(derivatives2[derivIndex]-derivatives1[derivIndex]);
                            derivTree.Append(new GH_Vector(deriv), treePath);
                        }
                    }
                }
            }

            // 9. Set output
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
