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
            : base("UniformDS", "UniformDS",
                "Generates a uniform lattice within by a design space",
                "IntraLattice2", "Frame")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.list);
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
            pManager.AddPointParameter("Nodes", "Nodes", "Lattice Nodes", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Struts", "Struts", "Strut curve network", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate data
            var topology = new List<Curve>();
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
            Brep brepDesignSpace = null;
            Mesh meshDesignSpace = null;
            if (!FrameTools.CastDesignSpace(ref designSpace, ref brepDesignSpace, ref meshDesignSpace))
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
            double[] N = new double[3] { nX, nY, nZ };

            // 5. Initialize nodeTree
            var nodeTree = new GH_Structure<GH_Point>();

            // 7. Prepare normalized unit cell topology
            var cell = new UnitCell();
            CellTools.ExtractTopology(ref topology, ref cell);  // converts list of lines into an adjacency list format (cellNodes and cellStruts)
            CellTools.NormaliseTopology(ref cell); // normalizes the unit cell (scaled to unit size and moved to origin)
            CellTools.FormatTopology(ref cell); // removes all duplicate struts and sets up reference for inter-cell nodes
            cell.Nodes.Transform(Transform.Scale(Plane.WorldXY, xCellSize, yCellSize, zCellSize));

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
                        // compute position vector
                        Vector3d V = u * vectorU + v * vectorV + w * vectorW;
                        Point3d currentPt = basePlane.Origin + V;

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
                            if (!nodeTree.PathExists(neighbours[0])) nodeTree.Append(new GH_Point(currentPt - vectorU), neighbours[0]);
                            if (!nodeTree.PathExists(neighbours[1])) nodeTree.Append(new GH_Point(currentPt - vectorV), neighbours[1]);
                            if (!nodeTree.PathExists(neighbours[2])) nodeTree.Append(new GH_Point(currentPt - vectorW), neighbours[2]);
                            if (!nodeTree.PathExists(neighbours[3])) nodeTree.Append(new GH_Point(currentPt + vectorU), neighbours[3]);
                            if (!nodeTree.PathExists(neighbours[4])) nodeTree.Append(new GH_Point(currentPt + vectorV), neighbours[4]);
                            if (!nodeTree.PathExists(neighbours[5])) nodeTree.Append(new GH_Point(currentPt + vectorW), neighbours[5]);
                            // same goes for the current node
                            if (!nodeTree.PathExists(currentPath))
                            {
                                nodeTree.Append(new GH_Point(currentPt), currentPath);
                            }

                        }
                    }
                }
            }


            // 3. Compute list of struts
            List<GH_Line> struts = new List<GH_Line>();

            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // We'll be needing the data tree path of the current node, and those of its neighbours
                        GH_Path currentPath = new GH_Path(u, v, w);

                        // Nere we create the actual struts
                        // First, make sure currentpath exists in the tree
                        if (nodeTree.PathExists(currentPath))
                        {
                            // Get neighbours!!
                            List<GH_Path> neighbourPaths = new List<GH_Path>();

                            foreach (GH_Path neighbourPath in neighbourPaths)
                            {
                                // Again, make sure the neighbourpath exists in the tree
                                if (nodeTree.PathExists(neighbourPath))
                                {
                                    Point3d node1 = nodeTree[currentPath][0].Value;
                                    Point3d node2 = nodeTree[neighbourPath][0].Value;

                                    // Set nodeInside status
                                    bool[] nodeInside = new bool[2] { false, false };
                                    // Could do this in the grid section (set bool values)
                                    if (brepDesignSpace != null)
                                    {
                                        if (brepDesignSpace.IsPointInside(nodeTree[currentPath][0].Value, Rhino.RhinoMath.SqrtEpsilon, true))
                                            nodeInside[0] = true;
                                        if (brepDesignSpace.IsPointInside(nodeTree[neighbourPath][0].Value, Rhino.RhinoMath.SqrtEpsilon, true))
                                            nodeInside[1] = true;
                                    }
                                    else if (meshDesignSpace != null)
                                    {
                                        if (meshDesignSpace.IsPointInside(nodeTree[currentPath][0].Value, Rhino.RhinoMath.SqrtEpsilon, true))
                                            nodeInside[0] = true;
                                        if (meshDesignSpace.IsPointInside(nodeTree[neighbourPath][0].Value, Rhino.RhinoMath.SqrtEpsilon, true))
                                            nodeInside[1] = true;
                                    }


                                    // Now perform checks
                                    // If neither node is inside, don't create a strut, skip to next loop
                                    if (!nodeInside[0] && !nodeInside[1])
                                        continue;
                                    // If both nodes are inside, add full strut
                                    else if (nodeInside[0] && nodeInside[1])
                                        struts.Add(new GH_Line(new Line(node1, node2)));
                                    // Else, strut requires trimming
                                    else
                                    {
                                        // We are going to find the intersection point with the design space
                                        Point3d[] intersectionPts = null;
                                        GH_Line testLine = null;

                                        // If brep design space
                                        if (brepDesignSpace != null)
                                        {
                                            Curve[] overlapCurves = null;   // dummy variable for CurveBrep call
                                            LineCurve strutToTrim = new LineCurve(new Line(node1, node2), 0, 1);
                                            // find intersection point
                                            Intersection.CurveBrep(strutToTrim, brepDesignSpace, Rhino.RhinoMath.SqrtEpsilon, out overlapCurves, out intersectionPts);
                                        }
                                        // If mesh design space
                                        else if (meshDesignSpace != null)
                                        {
                                            int[] faceIds;  // dummy variable for MeshLine call
                                            Line strutToTrim = new Line(node1, node2);
                                            // find intersection point
                                            intersectionPts = Intersection.MeshLine(meshDesignSpace, strutToTrim, out faceIds);
                                        }

                                        // Now, if an intersection point was found, trim the strut
                                        if (intersectionPts.Length > 0)
                                        {
                                            testLine = FrameTools.TrimStrut(node1, node2, intersectionPts[0], nodeInside);
                                            // if the strut was succesfully trimmed, add it to the list
                                            if (testLine != null) struts.Add(testLine);
                                        }

                                    }

                                }
                            }
                        }
                    }
                }
            }


            // 8. Set output
            DA.SetDataTree(0, nodeTree);
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