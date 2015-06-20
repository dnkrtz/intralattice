using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino;

namespace IntraLattice
{
    public class UniformBDS : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public UniformBDS()
            : base("UniformBDS", "UniformBDS",
                "Generates a uniform lattice grid in a Brep Design Space",
                "IntraLattice2", "Grid")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Design Space", "Brep", "Design space boundary representation", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Orientation Plane", "Plane", "Lattice orientation plane", GH_ParamAccess.item, Plane.WorldXY); // default is XY-plane
            pManager.AddNumberParameter("Size x", "Sx", "Size of unit cell (x)", GH_ParamAccess.item, 5); // default is 5
            pManager.AddNumberParameter("Size y", "Sy", "Size of unit cell (y)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Size z", "Sz", "Size of unit cell (z)", GH_ParamAccess.item, 5);
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
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve and validate data
            Brep BDS = null;
            Plane OrientationPlane = Plane.Unset;
            double Sx = 0;
            double Sy = 0;
            double Sz = 0;

            if (!DA.GetData(0, ref BDS)) { return; }
            if (!DA.GetData(1, ref OrientationPlane)) { return; }
            if (!DA.GetData(2, ref Sx)) { return; }
            if (!DA.GetData(3, ref Sy)) { return; }
            if (!DA.GetData(4, ref Sz)) { return; }

            if (!BDS.IsValid || !BDS.IsSolid) { return; }
            if (!OrientationPlane.IsValid) { return; }
            if (Sx == 0) { return; } 
            if (Sy == 0) { return; }
            if (Sz == 0) { return; }

            // Create bounding box
            Box BBox = new Box();
            BDS.GetBoundingBox(OrientationPlane, out BBox);

            // Get corner points
            Point3d[] BBoxCorner = BBox.GetCorners();

            double Lx = BBoxCorner[0].DistanceTo(BBoxCorner[1]);
            double Ly = BBoxCorner[0].DistanceTo(BBoxCorner[3]);
            double Lz = BBoxCorner[0].DistanceTo(BBoxCorner[4]);

            // Determine number of iterations required to fill the box
            int Nx = (int)Math.Ceiling(Lx / Sx); // Roundup to next integer if non-integer
            int Ny = (int)Math.Ceiling(Ly / Sy);
            int Nz = (int)Math.Ceiling(Lz / Sz);

            // Prepare input for grid generation
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();
            Plane BasePlane = new Plane(BBoxCorner[0], BBoxCorner[1], BBoxCorner[3]);

            // Define iteration vectors in each direction (accounting for Cell Size)
            Vector3d Vx = Sx * BasePlane.XAxis;
            Vector3d Vy = Sy * BasePlane.YAxis;
            Vector3d Vz = Sz * BasePlane.ZAxis;

            Point3d CurrentPt = new Point3d();

            // Create grid of points (as data tree)
            for (int i = 0; i <= Nx; i++)
            {
                for (int j = 0; j <= Ny; j++)
                {
                    for (int k = 0; k <= Nz; k++)
                    {
                        // Compute position vector
                        Vector3d V = i * Vx + j * Vy + k * Vz;
                        CurrentPt = BasePlane.Origin + V;

                        // Check if point is inside the Brep Design Space
                        if (BDS.IsPointInside(CurrentPt, RhinoMath.SqrtEpsilon, false))
                        {
                            // Neighbours of an inside node must be created (since they share a strut with an inside node, which we will be trimming)
                            // So before creating the node, we ensure that all its neighbours have been created
                            // This might seem excessive, but it's a robust approach
                            List<GH_Path> Neighbours = new List<GH_Path>();
                            Neighbours.Add(new GH_Path(i-1, j, k));
                            Neighbours.Add(new GH_Path(i, j-1, k));
                            Neighbours.Add(new GH_Path(i, j, k-1));
                            Neighbours.Add(new GH_Path(i+1, j, k));
                            Neighbours.Add(new GH_Path(i, j+1, k));
                            Neighbours.Add(new GH_Path(i, j, k+1));
                            GH_Path CurrentPath = new GH_Path(i, j, k);

                            // If the path doesn't exist, it hasn't been created, so create it
                            if (!GridTree.PathExists(Neighbours[0]))    GridTree.Append(new GH_Point(CurrentPt - Vx), Neighbours[0]);
                            if (!GridTree.PathExists(Neighbours[1]))    GridTree.Append(new GH_Point(CurrentPt - Vy), Neighbours[1]);
                            if (!GridTree.PathExists(Neighbours[2]))    GridTree.Append(new GH_Point(CurrentPt - Vz), Neighbours[2]);
                            if (!GridTree.PathExists(Neighbours[3]))    GridTree.Append(new GH_Point(CurrentPt + Vx), Neighbours[3]);
                            if (!GridTree.PathExists(Neighbours[4]))    GridTree.Append(new GH_Point(CurrentPt + Vy), Neighbours[4]);
                            if (!GridTree.PathExists(Neighbours[5]))    GridTree.Append(new GH_Point(CurrentPt + Vz), Neighbours[5]);
                            // Finally, same goes for the current node
                            if (!GridTree.PathExists(CurrentPath))      GridTree.Append(new GH_Point(CurrentPt), CurrentPath);
                        }

                    }
                }
            }
          

            // Output data
            DA.SetDataTree(0, GridTree);

        }

        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.tertiary;
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{d242b0c6-83a1-4795-8f8c-a32b1ac85fb3}"); }
        }
    }
}