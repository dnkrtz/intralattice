﻿using System;
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
using IntraLattice.CORE.Data;
using IntraLattice.CORE.Components;
using IntraLattice.CORE.Helpers;

// Summary:     This component generates a uniform lattice trimmed to the shape of design space
// ===============================================================================
// Details:     - Uniform lattice grids have unmorphed unit cells, and are trimmed by the design space.
//              - Design space may be a Mesh, Brep or Solid Surface.
//              - Orientation plane does not need to be centered at any particular location
// ===============================================================================
// Issues:      = Currently trimming for the meshes occasionally has really weird behaviour.. issue with Rhino's isInside method!
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)


namespace IntraLattice.CORE.Components
{
    public class UniformDSComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GridUniform class.
        /// </summary>
        public UniformDSComponent()
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
            pManager.AddNumberParameter("Tolerance", "Tol", "Smallest allowed strut length", GH_ParamAccess.item, 0.2);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Strut curve network", GH_ParamAccess.list);
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
            double minLength = 0; // the trim tolerance (i.e. minimum strut length)

            if (!DA.GetDataList(0, topology)) { return; }
            if (!DA.GetData(1, ref designSpace)) { return; }
            if (!DA.GetData(2, ref orientationPlane)) { return; }
            if (!DA.GetData(3, ref xCellSize)) { return; }
            if (!DA.GetData(4, ref yCellSize)) { return; }
            if (!DA.GetData(5, ref zCellSize)) { return; }
            if (!DA.GetData(6, ref minLength)) { return; }

            if (topology.Count < 2) { return; }
            if (!designSpace.IsValid) { return; }
            if (!orientationPlane.IsValid) { return; }
            if (xCellSize == 0) { return; } 
            if (yCellSize == 0) { return; }
            if (zCellSize == 0) { return; }
            if (minLength>=xCellSize || minLength>=yCellSize || minLength>=zCellSize)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tolerance parameter cannot be larger than the unit cell dimensions.");
                return;
            }
            // 2. Validate the design space
            int spaceType = FrameTools.ValidateSpace(ref designSpace);
            if (spaceType == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Design space must be a closed Brep, Mesh or Surface");
                return;
            }

            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
                
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
            var lattice = new Lattice(LatticeType.Uniform);

            // 6. Prepare normalized/formatted unit cell topology
            var cell = new LatticeCell(topology);
            cell.FormatTopology();          // sets up paths for inter-cell nodes

            // 7. Define iteration vectors in each direction (accounting for Cell Size)
            Vector3d vectorU = xCellSize * basePlane.XAxis;
            Vector3d vectorV = yCellSize * basePlane.YAxis;
            Vector3d vectorW = zCellSize * basePlane.ZAxis;

            // max kernel distance should be half the diagonal of a unit cell box
            double maxKernelDist = Math.Sqrt(Math.Pow(xCellSize,2)+Math.Pow(yCellSize,2)+Math.Pow(zCellSize,2))/2;

            // 8. Create grid of nodes (as data tree)
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        GH_Path treePath = new GH_Path(u, v, w);                // construct cell path in tree
                        var nodeList = lattice.Nodes.EnsurePath(treePath);      // fetch the list of nodes to append to, or initialise it

                        // this loop maps each node in the cell onto the UV-surface maps
                        for (int i = 0; i < cell.Nodes.Count; i++)
                        {
                            double usub = cell.Nodes[i].X; // u-position within unit cell (local)
                            double vsub = cell.Nodes[i].Y; // v-position within unit cell (local)
                            double wsub = cell.Nodes[i].Z; // w-position within unit cell (local)
                            double[] uvw = { u + usub, v + vsub, w + wsub }; // uvw-position (global)

                            // check if the node belongs to another cell (i.e. it's relative path points outside the current cell)
                            bool isOutsideCell = (cell.NodePaths[i][0] > 0 || cell.NodePaths[i][1] > 0 || cell.NodePaths[i][2] > 0);

                            if (isOutsideCell)
                                nodeList.Add(null);
                            else
                            {
                                // compute position vector
                                Vector3d V = uvw[0] * vectorU + uvw[1] * vectorV + uvw[2] * vectorW;
                                var newNode = new LatticeNode(basePlane.Origin + V);

                                // check if point is inside - use unstrict tolerance, meaning it can be outside the surface by the specified tolerance
                                bool isInside = FrameTools.IsPointInside(designSpace, newNode.Point3d, spaceType, tol);

                                if (isInside) newNode.State = LatticeNodeState.Inside;
                                else newNode.State = LatticeNodeState.Outside;

                                nodeList.Add(newNode);
                            }
                        }
                    }
                }
            }

            // 9. Compute list of struts
            var struts = lattice.UniformMapping(cell, designSpace, spaceType, N, minLength);
                
            // 10. Set output
            DA.SetDataList(0, struts);
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