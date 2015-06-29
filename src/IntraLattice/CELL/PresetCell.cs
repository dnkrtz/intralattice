using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

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
                "Description",
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
            // 0. Setup inputs
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            // 1. Retrieve input
            int cellType = 0;
            
            if (InputTools.ExecutionNum == 0) 
            {
                InputTools.TopoSelect(ref Component, ref GrasshopperDocument, 0, 0);
                InputTools.ExecutionNum += 1; 
            }

            if (!DA.GetData(0, ref cellType)) { return; }

            // Set cell size
            double cellSize = 5;
                  
            // Simple topologies
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
            var lines = new List<Curve>();
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

                        if (cellType == 0)
                        {
                            if (u < N[0]) neighbourPaths.Add(new GH_Path(u + 1, v, w));
                            if (v < N[1]) neighbourPaths.Add(new GH_Path(u, v + 1, w));
                            if (w < N[2]) neighbourPaths.Add(new GH_Path(u, v, w + 1));
                        }
                        if (cellType == 1)
                        {
                            if ((u < N[0]) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v + 1, w + 1));
                            if ((u > 0) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v - 1, w + 1));
                            if ((u < N[0]) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v - 1, w + 1));
                            if ((u > 0) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v + 1, w + 1));
                        }
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
                        if (cellType == 3)
                        {
                            if (u < N[0]) neighbourPaths.Add(new GH_Path(u + 1, v, w));
                            if (v < N[1]) neighbourPaths.Add(new GH_Path(u, v + 1, w));
                            if ((u < N[0]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v, w + 1));
                            if ((v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u, v - 1, w + 1));
                            if ((u < N[0]) && (v > 0) && (w < N[2])) neighbourPaths.Add(new GH_Path(u + 1, v - 1, w + 1));
                            if ((u > 0) && (v < N[1]) && (w < N[2])) neighbourPaths.Add(new GH_Path(u - 1, v + 1, w + 1));
                        }
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
                            lines.Add(new LineCurve(line));
                        }

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
                // return Resources.IconForThisComponent;
                return null;
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