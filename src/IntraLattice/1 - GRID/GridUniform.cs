using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.DocObjects;

// This component generates a trimmed uniform lattice grid
// =======================================================================
// Uniform lattice grids have unmorphed unit cells, and are trimmed by the design space.
// Points inside the design space, as well as their immediate neighbours, are generated.
// This is necessary since the struts between inner-outer points are trimmed later.
// ** Design space may be a Mesh, Brep or Solid Surface.
// ** Orientation plane does not need to be centered at any particular location

// Written by Aidan Kurtz (http://aidankurtz.com)


namespace IntraLattice
{
    public class GridUniform : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GridUniform class.
        /// </summary>
        public GridUniform()
            : base("UniformTrimmed", "UniformTrim",
                "Generates a uniform lattice grid within by a design space",
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
            // 1. Retrieve and validate data
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
            if (!orientationPlane.IsValid) { return; }
            if (xCellSize == 0) { return; } 
            if (yCellSize == 0) { return; }
            if (zCellSize == 0) { return; }

            // 2. Validate the design space
            Brep brepDesignSpace = null;
            Mesh meshDesignSpace = null;
            //    If brep design space, cast as such
            if (designSpace.ObjectType == ObjectType.Brep)
                brepDesignSpace = (Brep)designSpace;
            //    If mesh design space, cast as such
            else if (designSpace.ObjectType == ObjectType.Mesh)
                meshDesignSpace = (Mesh)designSpace;
            //    If solid surface, convert to brep
            else if (designSpace.ObjectType == ObjectType.Surface)
            {
                Surface testSpace = (Surface)designSpace;
                if(testSpace.IsSolid) brepDesignSpace = testSpace.ToBrep();
            }
            //    Else the design space is unacceptable
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Design space must be a Brep, Mesh or Closed Surface");
                return;
            }

            // 3. Compute oriented bounding box and its corner points
            Box bBox = new Box();
            designSpace.GetBoundingBox(orientationPlane, out bBox);
            Point3d[] bBoxCorners = bBox.GetCorners();

            // 4. Determine number of iterations required to fill the box
            double xLength = bBoxCorners[0].DistanceTo(bBoxCorners[1]);
            double yLength = bBoxCorners[0].DistanceTo(bBoxCorners[3]);
            double zLength = bBoxCorners[0].DistanceTo(bBoxCorners[4]);
            int nX = (int)Math.Ceiling(xLength / xCellSize); // Roundup to next integer if non-integer
            int nY = (int)Math.Ceiling(yLength / yCellSize);
            int nZ = (int)Math.Ceiling(zLength / zCellSize);

            // 5. Prepare grid
            var gridTree = new GH_Structure<GH_Point>();
            Plane basePlane = new Plane(bBoxCorners[0], bBoxCorners[1], bBoxCorners[3]);

            // 6. Define iteration vectors in each direction (accounting for Cell Size)
            Vector3d vectorX = xCellSize * basePlane.XAxis;
            Vector3d vectorY = yCellSize * basePlane.YAxis;
            Vector3d vectorZ = zCellSize * basePlane.ZAxis;

            Point3d currentPt = new Point3d();

            // 7. Create grid of points (as data tree)
            for (int u = 0; u <= nX; u++)
            {
                for (int v = 0; v <= nY; v++)
                {
                    for (int w = 0; w <= nZ; w++)
                    {
                        // compute position vector
                        Vector3d V = u * vectorX + v * vectorY + w * vectorZ;
                        currentPt = basePlane.Origin + V;

                        // check if point is inside
                        bool isInside = false;

                        // if design space is a BREP
                        if (brepDesignSpace != null)
                            // check if it is inside the space (within unstrict tolerance, meaning it can be outside the surface by the specified tolerance)
                            isInside = brepDesignSpace.IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);
                        // if design space is a MESH
                        if (meshDesignSpace != null)
                            isInside = meshDesignSpace.IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);

                        // if point is inside the design space, we add it to the datatree,
                        // for the reason stated above, we must also ensure that all neighbours of inside nodes are included
                        if (isInside)
                        {
                            // this might seem excessive, but it's a robust approach
                            List<GH_Path> neighbours = new List<GH_Path>();
                            neighbours.Add(new GH_Path(u - 1, v, w));
                            neighbours.Add(new GH_Path(u, v - 1, w));
                            neighbours.Add(new GH_Path(u, v, w - 1));
                            neighbours.Add(new GH_Path(u + 1, v, w));
                            neighbours.Add(new GH_Path(u, v + 1, w));
                            neighbours.Add(new GH_Path(u, v, w + 1));
                            GH_Path currentPath = new GH_Path(u, v, w);

                            // if the path doesn't exist, it hasn't been added, so add it
                            if (!gridTree.PathExists(neighbours[0])) gridTree.Append(new GH_Point(currentPt - vectorX), neighbours[0]);
                            if (!gridTree.PathExists(neighbours[1])) gridTree.Append(new GH_Point(currentPt - vectorY), neighbours[1]);
                            if (!gridTree.PathExists(neighbours[2])) gridTree.Append(new GH_Point(currentPt - vectorZ), neighbours[2]);
                            if (!gridTree.PathExists(neighbours[3])) gridTree.Append(new GH_Point(currentPt + vectorX), neighbours[3]);
                            if (!gridTree.PathExists(neighbours[4])) gridTree.Append(new GH_Point(currentPt + vectorY), neighbours[4]);
                            if (!gridTree.PathExists(neighbours[5])) gridTree.Append(new GH_Point(currentPt + vectorZ), neighbours[5]);
                            // same goes for the current node
                            if (!gridTree.PathExists(currentPath))
                            {
                                gridTree.Append(new GH_Point(currentPt), currentPath);
                            }

                        }
                    }
                }

            }
            // 8. Set output
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