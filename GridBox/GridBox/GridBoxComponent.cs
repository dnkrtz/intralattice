using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component generates a simple cartesian 3D lattice grid.
// Includes comments for tyros, explaining the purpose of each method of a C# Grasshopper Component (GH_Component).

namespace GridBox
{
    public class GridBoxComponent : GH_Component
    {

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public GridBoxComponent()
            : base("GridBox", "GridBox",
                "Generates a lattice grid box.",
                "IntraLattice2", "Grid")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Size x", "Sx", "Size of unit cell (x)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Size y", "Sy", "Size of unit cell (y)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Size z", "Sz", "Size of unit cell (z)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number x", "Nx", "Number of unit cells (x)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number y", "Ny", "Number of unit cells (y)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number z", "Nz", "Number of unit cells (z)", GH_ParamAccess.item, 5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables and assign initial invalid data.
            //    This way, if the input parameters fail to supply valid data, we know when to abort.
            double Sx = 0;
            double Sy = 0;
            double Sz = 0;
            double Nx = 0;
            double Ny = 0;
            double Nz = 0;

            // 2. Retrieve input data.
            if (!DA.GetData(0, ref Sx)) { return; }
            if (!DA.GetData(1, ref Sy)) { return; }
            if (!DA.GetData(2, ref Sz)) { return; }
            if (!DA.GetData(3, ref Nx)) { return; }
            if (!DA.GetData(4, ref Ny)) { return; }
            if (!DA.GetData(5, ref Nz)) { return; }

            // 3. If data is invalid, we need to abort.
            if (Sx == 0) { return; }
            if (Sy == 0) { return; }
            if (Sz == 0) { return; }
            if (Nx == 0) { return; }
            if (Ny == 0) { return; }
            if (Nz == 0) { return; }

            //// 4. Let's cook some pasta
            // Declare gh_structure data tree
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();

            // Assign BasePlane
            Plane BasePlane = Plane.WorldXY;

            // Create grid of points (as data tree)
            for (int i = 0; i <= Nz; i++)
            {
                for (int j = 0; j <= Ny; j++)
                {
                    for (int k = 0; k <= Nx; k++)
                    {
                        // Compute position vector
                        Vector3d V = new Vector3d(i * Sx, j * Sy, k * Sz);
                        
                        Point3d NewPt = BasePlane.Origin + V;

                        GH_Path TreePath = new GH_Path(0, i, j);            // Construct path in tree
                        GridTree.Append(new GH_Point(NewPt), TreePath);     // Add point to tree
                    }
                }
            }

            // 5. Output data
            DA.SetDataTree(0, GridTree);
        }
        
        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
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
            get { return new Guid("{0a7a804a-88d8-401a-8f48-44d0dea2e6b6}"); }
        }
    }
}
