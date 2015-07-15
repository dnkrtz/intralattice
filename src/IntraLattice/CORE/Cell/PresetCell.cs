using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino;
using IntraLattice.Properties;

namespace IntraLattice
{
    public class PresetCell : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the PresetCell class.
        /// </summary>
        public PresetCell()
            : base("PresetCell", "PresetCell",
                "Built-in selection of unit cell topologies.",
                "IntraLattice2", "Cell")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Cell Tye", "Type", "Unit cell topology type", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Topology", "Topo", "Line topology", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 0. Setup input
            Component = this;
            GrasshopperDocument = this.OnPingDocument();
            //    Generate default input menu
            if (Component.Params.Input[0].SourceCount == 0) InputTools.TopoSelect(ref Component, ref GrasshopperDocument, 0, 11);

            // 1. Retrieve input
            int cellType = 0;
            if (!DA.GetData(0, ref cellType)) { return; }

            var lines = new List<Line>();

            // Set cell size
            double cellSize = 5;

            // Simple topologies
            if (cellType <= 4)
            {
                int[] N = new int[] { 1, 1, 1 };

                // Make node grid
                var nodeGrid = new GH_Structure<GH_Point>();
                for (int u = 0; u <= N[0]; u++)
                {
                    for (int v = 0; v <= N[1]; v++)
                    {
                        for (int w = 0; w <= N[2]; w++)
                        {
                            Vector3d V = u * Plane.WorldXY.XAxis + v * Plane.WorldXY.YAxis + w * Plane.WorldXY.ZAxis;
                            Point3d node = Plane.WorldXY.Origin + V * cellSize;

                            GH_Path currentPath = new GH_Path(u, v, w);
                            nodeGrid.Append(new GH_Point(node), currentPath);
                        }
                    }
                }

                // Make struts
             
                for (int u = 0; u <= N[0]; u++)
                {
                    for (int v = 0; v <= N[1]; v++)
                    {
                        for (int w = 0; w <= N[2]; w++)
                        {
                            // We'll be needing the data tree path of the current node, and those of its neighbours
                            GH_Path currentPath = new GH_Path(u, v, w);
                            if (!nodeGrid.PathExists(currentPath)) continue; // if current path doesnt exist in tree, skip loop

                            List<GH_Path> neighbourPaths = new List<GH_Path>();
                            //Grid
                            if (cellType == 0)
                            {
                                if (u < N[0]) neighbourPaths.Add(new GH_Path(u + 1, v, w));
                                if (v < N[1]) neighbourPaths.Add(new GH_Path(u, v + 1, w));
                                if (w < N[2]) neighbourPaths.Add(new GH_Path(u, v, w + 1));
                            }
                            //X
                            if (cellType == 1)
                            {
                                if ((u < N[0]) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v + 1, w + 1));
                                if ((u > 0) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v - 1, w + 1));
                                if ((u < N[0]) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v - 1, w + 1));
                                if ((u > 0) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v + 1, w + 1));
                            }
                            //Star
                            if (cellType == 2)
                            {
                                if (u < N[0]) neighbourPaths.Add(new GH_Path(u + 1, v, w));
                                if (v < N[1]) neighbourPaths.Add(new GH_Path(u, v + 1, w));
                                if (w < N[2]) neighbourPaths.Add(new GH_Path(u, v, w + 1));
                                if ((u < N[0]) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v + 1, w + 1));
                                if ((u > 0) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v - 1, w + 1));
                                if ((u < N[0]) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v - 1, w + 1));
                                if ((u > 0) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v + 1, w + 1));
                            }
                            //Cross
                            if (cellType == 3)
                            {
                                if (u < N[0]) neighbourPaths.Add(new GH_Path(u + 1, v, w));
                                if (v < N[1]) neighbourPaths.Add(new GH_Path(u, v + 1, w));
                                if ((u < N[0]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v, w + 1));
                                if ((v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u, v - 1, w + 1));
                                if ((u < N[0]) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v - 1, w + 1));
                                if ((u > 0) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v + 1, w + 1));
                            }
                            //Cross2
                            if (cellType == 4)
                            {
                                if ((u < N[0]) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v + 1, w + 1));
                                if ((u > 0) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v - 1, w + 1));
                                if ((u < N[0]) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v - 1, w + 1));
                                if ((u > 0) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v + 1, w + 1));
                            }


                            foreach (GH_Path neighbourPath in neighbourPaths)
                            {
                                if (!nodeGrid.PathExists(neighbourPath)) continue;  // if neighbour path doesnt exist in node grid, skip loop
                                Line line = new Line(nodeGrid[currentPath][0].Value, nodeGrid[neighbourPath][0].Value);
                                lines.Add(line);
                            }

                        }
                    }
                }

            }
            else
            {
                List<Point3d> pt = new List<Point3d>();
                // Vintiles
                if (cellType == 5)
                {
                    foreach (double i in new double[2]{0, cellSize})
                    {
                        pt.Add(new Point3d(0, cellSize / 4, i));
                        pt.Add(new Point3d(0, 3 * cellSize / 4, i));
                        pt.Add(new Point3d(cellSize / 4, cellSize, i));
                        pt.Add(new Point3d(3 * cellSize / 4, cellSize, i));
                        pt.Add(new Point3d(cellSize, 3 * cellSize / 4, i));
                        pt.Add(new Point3d(cellSize, cellSize / 4, i));
                        pt.Add(new Point3d(3 * cellSize / 4, 0, i));
                        pt.Add(new Point3d(cellSize / 4, 0, i));
                    }

                    foreach (double i in new double[2] { cellSize / 4.0, 3.0 * cellSize / 4.0 })
                    {
                        pt.Add(new Point3d(0, cellSize / 2, i));
                        pt.Add(new Point3d(cellSize / 2, cellSize, i));
                        pt.Add(new Point3d(cellSize, cellSize / 2, i));
                        pt.Add(new Point3d(cellSize / 2, 0, i));
                    }

                    foreach (double i in new double[2] { cellSize / 4, 3 * cellSize / 4 })
                    {
                        pt.Add(new Point3d(cellSize / 2, i, cellSize / 2));
                    }

                    foreach (double i in new double[2] { cellSize / 4, 3 * cellSize / 4 })
                    {
                        pt.Add(new Point3d(i, cellSize / 2, cellSize / 2));
                    }

                    foreach (int i in new int[3] { 0, 1, 26 })
                    {
                        lines.Add(new Line(pt[16], pt[i]));
                    }

                    foreach (int i in new int[3] { 2, 3, 25 })
                    {
                        lines.Add(new Line(pt[17], pt[i]));
                    }

                    foreach (int i in new int[3] { 4, 5, 27 })
                    {
                        lines.Add(new Line(pt[18], pt[i]));
                    }

                    foreach (int i in new int[3] { 6, 7, 24 })
                    {
                        lines.Add(new Line(pt[19], pt[i]));
                    }

                    foreach (int i in new int[3] { 8, 9, 26 })
                    {
                        lines.Add(new Line(pt[20], pt[i]));
                    }

                    foreach (int i in new int[3] { 10, 11, 25 })
                    {
                        lines.Add(new Line(pt[21],pt[i]));
                    }

                    foreach (int i in new int[3] { 12, 13, 27 })
                    {
                        lines.Add(new Line(pt[22], pt[i]));
                    }

                    foreach (int i in new int[3] { 14, 15, 24 })
                    {
                        lines.Add(new Line(pt[23], pt[i]));
                    }
                    foreach (int i in new int[2] { 24, 25 })
                    {
                        lines.Add(new Line(pt[26], pt[i]));
                        lines.Add(new Line(pt[27], pt[i]));
                    }
                    foreach (int i in new int[6] { 1, 3, 5, 9, 11, 13 })
                    {
                        lines.Add(new Line(pt[i], pt[i + 1]));
                    }

                    lines.Add(new Line(pt[0], pt[7]));
                    lines.Add(new Line(pt[8], pt[15]));
                }
                // Octahedral
                if (cellType == 6) 
                {
                    //corner points
                    pt.Add(new Point3d(0,0,0));
                    pt.Add(new Point3d(0, cellSize, 0));
                    pt.Add(new Point3d(cellSize, cellSize, 0));
                    pt.Add(new Point3d(cellSize, 0, 0));
                    pt.Add(new Point3d(0, 0, cellSize));
                    pt.Add(new Point3d(0, cellSize, cellSize));
                    pt.Add(new Point3d(cellSize, cellSize, cellSize));
                    pt.Add(new Point3d(cellSize, 0, cellSize));
                    //face center points
                    pt.Add(new Point3d(cellSize, cellSize/2, cellSize/2));
                    pt.Add(new Point3d(cellSize/2, cellSize, cellSize/2));
                    pt.Add(new Point3d(0, cellSize/2, cellSize / 2));
                    pt.Add(new Point3d(cellSize / 2, 0, cellSize / 2));
                    pt.Add(new Point3d(cellSize / 2, cellSize/2, 0));
                    pt.Add(new Point3d(cellSize / 2, cellSize/2, cellSize));
                    
                    foreach(int i in new int[8]{0,1,2,3,8,9,10,11})
                    {
                        lines.Add(new Line(pt[12], pt[i]));
                    }

                    foreach (int i in new int[8] { 4,5, 6, 7, 8, 9, 10, 11 })
                    {
                        lines.Add(new Line(pt[13], pt[i]));
                    }

                    foreach (int i in new int[4] { 0, 1, 4, 5 })
                    {
                        lines.Add(new Line(pt[10], pt[i]));
                    }

                    foreach (int i in new int[6] { 1, 2, 5, 6, 8, 10 })
                    {
                        lines.Add(new Line(pt[9], pt[i]));
                    }

                    foreach (int i in new int[4] { 2,3,6,7 })
                    {
                        lines.Add(new Line(pt[8], pt[i]));
                    }

                    foreach (int i in new int[6] { 0,3,4,7,8,10 })
                    {
                        lines.Add(new Line(pt[11], pt[i]));
                    }
                }

            }

            CellTools.FixIntersections(ref lines);

            // 8. Set output
            DA.SetDataList(0, lines);
            
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.atom;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{508cc705-bc5b-42a9-8100-c1e364f3b83d}"); }
        }
    }
}