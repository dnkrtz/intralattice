using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino;
using Rhino.DocObjects;
using Rhino.Collections;
using Rhino.Geometry.Intersect;
using IntraLattice.Properties;
using Grasshopper;

// Summary:     This component generates a uniform lattice trimmed to the shape of design space
// ===============================================================================
// Details:     - Uniform lattice grids have unmorphed unit cells, and are trimmed by the design space.
//              - Design space may be a Mesh, Brep or Solid Surface.
//              - Orientation plane does not need to be centered at any particular location
// ===============================================================================
// Issues:      = Currently trimming for the meshes occasionally has really weird behaviour.. issue with Rhino's isInside method!
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)


namespace IntraLattice
{
    public class GridUniform : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GridUniform class.
        /// </summary>
        public GridUniform()
            : base("Uniform DS", "UniformDS",
                "Generates a uniform lattice within by a design space",
                "IntraLattice2", "Frame")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.list);
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
            pManager.AddCurveParameter("Struts", "Struts", "Strut curve network", GH_ParamAccess.list);
            pManager.AddPointParameter("Nodes", "Nodes", "Lattice Nodes", GH_ParamAccess.tree);
            pManager.HideParameter(1); // Do not display the 'Nodes' output points
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate data
            var topology = new List<Line>();
            GeometryBase designSpace = null;
            Plane orientationPlane = Plane.Unset;
            double xCellSize = 0;
            double yCellSize = 0;
            double zCellSize = 0;

            if (!DA.GetDataList(0, topology)) { return; }
            if (!DA.GetData(1, ref designSpace)) { return; }
            if (!DA.GetData(2, ref orientationPlane)) { return; }
            if (!DA.GetData(3, ref xCellSize)) { return; }
            if (!DA.GetData(4, ref yCellSize)) { return; }
            if (!DA.GetData(5, ref zCellSize)) { return; }

            if (topology.Count < 2) { return; }
            if (!designSpace.IsValid) { return; }
            if (!orientationPlane.IsValid) { return; }
            if (xCellSize == 0) { return; } 
            if (yCellSize == 0) { return; }
            if (zCellSize == 0) { return; }

            // 2. Validate the design space
            int spaceType = FrameTools.CastDesignSpace(ref designSpace);
            if (spaceType == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Design space must be a Brep, Mesh or Closed Surface");
                return;
            }
                
            // 3. Compute oriented bounding box and its corner points
            Box bBox = new Box();
            designSpace.GetBoundingBox(orientationPlane, out bBox);
            Point3d[] bBoxCorners = bBox.GetCorners();
            //    Set basePlane based on the bounding box
            Plane basePlane = new Plane(bBoxCorners[0], bBoxCorners[1], bBoxCorners[3]);

            // 4. Determine number of iterations required to fill the box, and package into array
            double xLength = bBoxCorners[0].DistanceTo(bBoxCorners[1]);
            double yLength = bBoxCorners[0].DistanceTo(bBoxCorners[3]);
            double zLength = bBoxCorners[0].DistanceTo(bBoxCorners[4]);
            int nX = (int)Math.Ceiling(xLength / xCellSize); // Roundup to next integer if non-integer
            int nY = (int)Math.Ceiling(yLength / yCellSize);
            int nZ = (int)Math.Ceiling(zLength / zCellSize);
            float[] N = new float[3] { nX, nY, nZ };

            // 5. Initialize nodeTree
            var nodeTree = new DataTree<Point3d>();     // will contain the lattice nodes
            var stateTree = new DataTree<Boolean>();    // will contain the node states in a parallel tree (true if node is inside design space)

            // 7. Prepare normalized unit cell topology
            var cell = new UnitCell();
            CellTools.FixIntersections(ref topology);
            CellTools.ExtractTopology(ref topology, ref cell);  // converts list of lines into an adjacency list format (cellNodes and cellStruts)
            CellTools.NormaliseTopology(ref cell); // normalizes the unit cell (scaled to unit size and moved to origin)
            CellTools.FormatTopology(ref cell); // removes all duplicate struts and sets up reference for inter-cell nodes

            // 6. Define iteration vectors in each direction (accounting for Cell Size)
            Vector3d vectorU = xCellSize * basePlane.XAxis;
            Vector3d vectorV = yCellSize * basePlane.YAxis;
            Vector3d vectorW = zCellSize * basePlane.ZAxis;

            // 7. Create grid of nodes (as data tree)
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // this loop maps each node in the cell onto the UV-surface maps
                        for (int i = 0; i < cell.Nodes.Count; i++)
                        {
                            // if the node belongs to another cell (i.e. it's relative path points outside the current cell)
                            if (cell.NodePaths[i][0] + cell.NodePaths[i][1] + cell.NodePaths[i][2] > 0)
                                continue;

                            double usub = cell.Nodes[i].X; // u-position within unit cell
                            double vsub = cell.Nodes[i].Y; // v-position within unit cell
                            double wsub = cell.Nodes[i].Z; // w-position within unit cell

                            // compute position vector
                            Vector3d V = (u+usub) * vectorU + (v+vsub) * vectorV + (w+wsub) * vectorW;
                            Point3d currentPt = basePlane.Origin + V;

                            // u,v,w is the cell grid. the 'i' index is for different nodes in each cell.
                            // create current node
                            GH_Path currentPath = new GH_Path(u, v, w, i);
                            if (!nodeTree.PathExists(currentPath))
                                nodeTree.Add(currentPt, currentPath);

                            // check if point is inside - use unstrict tolerance, meaning it can be outside the surface by the specified tolerance
                            bool isInside = false;
                            
                            switch (spaceType)
                            {
                                case 1: // Brep design space
                                    isInside = ((Brep)designSpace).IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);
                                    break;
                                case 2: // Mesh design space
                                    isInside = ((Mesh)designSpace).IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);
                                    break;
                                case 3: // Solid surface design space (must be converted to brep)
                                    isInside = ((Surface)designSpace).ToBrep().IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);
                                    break;
                            }

                            // store wether the pt is inside or outside
                            if (isInside)
                                stateTree.Add(true, currentPath);
                            else
                                stateTree.Add(false, currentPath);

                        }
                    }
                }
            }

            // 3. Compute list of struts
            var struts = new List<Curve>();
            FrameTools.UniformMapping(ref struts, ref nodeTree, ref stateTree, ref cell, designSpace, spaceType, N);           
                
            // 8. Set output
            DA.SetDataList(0, struts);
            DA.SetDataTree(1, nodeTree);
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
                //return Resources.checkd;
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