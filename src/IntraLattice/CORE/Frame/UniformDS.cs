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

// This component generates a trimmed uniform lattice grid
// =======================================================================
// Uniform lattice grids have unmorphed unit cells, and are trimmed by the design space.
// Points inside the design space, as well as their immediate neighbours, are generated.
// This is necessary since the struts between inner-outer points are trimmed later.
// ** Design space may be a Mesh, Brep or Solid Surface.
// ** Orientation plane does not need to be centered at any particular location

// Currently doesn't work well for meshes.. issues with intersection and isInside

// Written by Aidan Kurtz (http://aidankurtz.com)


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
            pManager.HideParameter(1);
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
            float[] N = new float[3] { nX, nY, nZ };

            // 5. Initialize nodeTree
            var nodeTree = new GH_Structure<GH_Point>();
            var stateTree = new GH_Structure<GH_Boolean>(); // true if point is inside design space

            // 7. Prepare normalized unit cell topology
            var cell = new UnitCell();
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
                                nodeTree.Append(new GH_Point(currentPt), currentPath);

                            // check if point is inside
                            bool isInside = false;
                            // if design space is a BREP
                            if (brepDesignSpace != null)
                                // check if it is inside the space (within unstrict tolerance, meaning it can be outside the surface by the specified tolerance)
                                isInside = brepDesignSpace.IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);
                            // if design space is a MESH
                            if (meshDesignSpace != null)
                                isInside = meshDesignSpace.IsPointInside(currentPt, RhinoMath.SqrtEpsilon, false);

                            // store wether the pt is inside or outside
                            if (isInside)
                                stateTree.Append(new GH_Boolean(true), currentPath);
                            else
                                stateTree.Append(new GH_Boolean(false), currentPath);

                        }
                    }
                }
            }

            // 3. Compute list of struts
            var struts = new List<LineCurve>();
            var nodesToRemove = new List<GH_Path>();

            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // we're inside a unit cell
                        // loop through all pairs of nodes that make up struts
                        foreach (IndexPair cellStrut in cell.StrutNodes)
                        {
                            // prepare the path of the two nodes (path in tree)
                            int[] IRel = cell.NodePaths[cellStrut.I];  // relative path of nodes (with respect to current unit cell)
                            int[] JRel = cell.NodePaths[cellStrut.J];
                            GH_Path IPath = new GH_Path(u + IRel[0], v + IRel[1], w + IRel[2], IRel[3]);
                            GH_Path JPath = new GH_Path(u + JRel[0], v + JRel[1], w + JRel[2], JRel[3]);

                            // make sure both nodes exist (will be false at boundaries)
                            if (nodeTree.PathExists(IPath) && nodeTree.PathExists(JPath))
                            {
                                Point3d node1 = nodeTree[IPath][0].Value;
                                Point3d node2 = nodeTree[JPath][0].Value;

                                // Determine inside/outside state of both nodes
                                bool[] nodeInside = new bool[2];
                                nodeInside[0] = stateTree[IPath][0].Value;
                                nodeInside[1] = stateTree[JPath][0].Value;

                                // If neither node is inside, remove them and skip to next loop
                                if (!nodeInside[0] && !nodeInside[1])
                                {
                                    nodesToRemove.Add(IPath);
                                    nodesToRemove.Add(JPath);
                                    continue;
                                }
                                // If both nodes are inside, add full strut
                                else if (nodeInside[0] && nodeInside[1])
                                    struts.Add(new LineCurve(node1, node2));
                                // Else, strut requires trimming
                                else
                                {
                                    // We are going to find the intersection point with the design space
                                    Point3d[] intersectionPts = null;
                                    LineCurve testLine = null;

                                    // If brep design space
                                    if (brepDesignSpace != null)
                                    {
                                        Curve[] overlapCurves = null;   // dummy variable for CurveBrep call
                                        LineCurve strutToTrim = new LineCurve(node1, node2);
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
                                        testLine = FrameTools.TrimStrut(ref nodeTree, ref stateTree, ref nodesToRemove, IPath, JPath, intersectionPts[0], nodeInside);
                                        // if the strut was succesfully trimmed, add it to the list
                                        if (testLine != null) struts.Add(testLine);
                                    }

                                }
                            }
                        }
                    }
                }
            }

            foreach (GH_Path nodeToRemove in nodesToRemove)
            {
                if (nodeTree.PathExists(nodeToRemove))
                {
                    if (nodeTree[nodeToRemove].Count > 1)  // if node is a swap node (replaced by intersection pt)
                    {
                        nodeTree[nodeToRemove].RemoveAt(0);
                        stateTree[nodeToRemove].RemoveAt(0);
                    }
                    else if (!stateTree[nodeToRemove][0].Value) // if node is outside
                        nodeTree.RemovePath(nodeToRemove);
                }
            }
                
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