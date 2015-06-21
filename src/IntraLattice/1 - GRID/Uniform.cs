using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.DocObjects;

namespace IntraLattice
{
    public class Uniform : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Uniform()
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
            pManager.AddGeometryParameter("Design Space", "DS", "Design Space (Brep or Mesh)", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Orientation Plane", "Plane", "Lattice orientation plane", GH_ParamAccess.item, Plane.WorldXY); // default is XY-plane
            pManager.AddNumberParameter("Cell Size ( x )", "CSx", "Size of unit cell (x)", GH_ParamAccess.item, 5); // default is 5
            pManager.AddNumberParameter("Cell Size ( y )", "CSy", "Size of unit cell (y)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Cell Size ( z )", "CSz", "Size of unit cell (z)", GH_ParamAccess.item, 5);
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
            GeometryBase designSpace = null;
            Plane orientationPlane = Plane.Unset;
            double xCellSize = 0;
            double yCellSize = 0;
            double zCellSize = 0;

            if (!DA.GetData(0, ref designSpace)) { return; }
            if (!DA.GetData(1, ref orientationPlane)) { return; }
            if (!DA.GetData(2, ref xCellSize)) { return; }
            if (!DA.GetData(3, ref yCellSize)) { return; }
            if (!DA.GetData(4, ref zCellSize)) { return; }

            if (!designSpace.IsValid) { return; }
            if (designSpace.ObjectType != ObjectType.Brep && designSpace.ObjectType != ObjectType.Mesh) { return; }   // design space must be a Brep or a Mesh
            if (!orientationPlane.IsValid) { return; }
            if (xCellSize == 0) { return; } 
            if (yCellSize == 0) { return; }
            if (zCellSize == 0) { return; }

            // Create bounding box
            Box bBox = new Box();
            designSpace.GetBoundingBox(orientationPlane, out bBox);

            // Get corner points
            Point3d[] bBoxCorners = bBox.GetCorners();

            double xLength = bBoxCorners[0].DistanceTo(bBoxCorners[1]);
            double yLength = bBoxCorners[0].DistanceTo(bBoxCorners[3]);
            double zLength = bBoxCorners[0].DistanceTo(bBoxCorners[4]);

            // Determine number of iterations required to fill the box
            int nX = (int)Math.Ceiling(xLength / xCellSize); // Roundup to next integer if non-integer
            int nY = (int)Math.Ceiling(yLength / yCellSize);
            int nZ = (int)Math.Ceiling(zLength / zCellSize);

            // Prepare input for grid generation
            GH_Structure<GH_Point> gridTree = new GH_Structure<GH_Point>();
            Plane basePlane = new Plane(bBoxCorners[0], bBoxCorners[1], bBoxCorners[3]);

            // Define iteration vectors in each direction (accounting for Cell Size)
            Vector3d vectorX = xCellSize * basePlane.XAxis;
            Vector3d vectorY = yCellSize * basePlane.YAxis;
            Vector3d vectorZ = zCellSize * basePlane.ZAxis;

            Point3d currentPt = new Point3d();

            // Create grid of points (as data tree)
            for (int i = 0; i <= nX; i++)
            {
                for (int j = 0; j <= nY; j++)
                {
                    for (int k = 0; k <= nZ; k++)
                    {
                        // Compute position vector
                        Vector3d V = i * vectorX + j * vectorY + k * vectorZ;
                        currentPt = basePlane.Origin + V;

                        // Cast according to type (design space could be mesh or brep)
                        Boolean isInside = false;
                        if (designSpace.ObjectType == ObjectType.Brep)       isInside = ((Brep)designSpace).IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);                           
                        else if (designSpace.ObjectType == ObjectType.Mesh)  isInside = ((Mesh)designSpace).IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);

                        // Check if point is inside the Brep Design Space
                        if (isInside)
                        {
                            // Neighbours of an inside node must be created (since they share a strut with an inside node, which we will be trimming)
                            // So before creating the node, we ensure that all its neighbours have been created
                            // This might seem excessive, but it's a robust approach
                            List<GH_Path> neighbours = new List<GH_Path>();
                            neighbours.Add(new GH_Path(i-1, j, k));
                            neighbours.Add(new GH_Path(i, j-1, k));
                            neighbours.Add(new GH_Path(i, j, k-1));
                            neighbours.Add(new GH_Path(i+1, j, k));
                            neighbours.Add(new GH_Path(i, j+1, k));
                            neighbours.Add(new GH_Path(i, j, k+1));
                            GH_Path currentPath = new GH_Path(i, j, k);

                            // If the path doesn't exist, it hasn't been created, so create it
                            if (!gridTree.PathExists(neighbours[0]))    gridTree.Append(new GH_Point(currentPt - vectorX), neighbours[0]);
                            if (!gridTree.PathExists(neighbours[1]))    gridTree.Append(new GH_Point(currentPt - vectorY), neighbours[1]);
                            if (!gridTree.PathExists(neighbours[2]))    gridTree.Append(new GH_Point(currentPt - vectorZ), neighbours[2]);
                            if (!gridTree.PathExists(neighbours[3]))    gridTree.Append(new GH_Point(currentPt + vectorX), neighbours[3]);
                            if (!gridTree.PathExists(neighbours[4]))    gridTree.Append(new GH_Point(currentPt + vectorY), neighbours[4]);
                            if (!gridTree.PathExists(neighbours[5]))    gridTree.Append(new GH_Point(currentPt + vectorZ), neighbours[5]);
                            // Finally, same goes for the current node
                            if (!gridTree.PathExists(currentPath))      gridTree.Append(new GH_Point(currentPt), currentPath);
                        }

                    }
                }
            }
          

            // Output data
            DA.SetDataTree(0, gridTree);

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