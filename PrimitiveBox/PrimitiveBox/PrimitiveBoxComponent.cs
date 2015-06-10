using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace PrimitiveBox
{
    public class PrimitiveBoxComponent : GH_Component
    {
        /// Public constructor
        public PrimitiveBoxComponent()
            : base("PrimitiveBox", "PBox",
                "Generates a simple lattice box",
                "INTRA|LATTICE", "Wireframe")
        {
        }

        /// Registers all the input parameters for this component.
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Cell Size", "cellSize", "Unit cell dimension (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number x", "xNum", "Number of unit cells in x-dir", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number y", "yNum", "Number of unit cells in y-dir", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number z", "zNum", "Number of unit cells in z-dir", GH_ParamAccess.item);
        }

        /// Registers all the output parameters for this component.
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Point Grid", GH_ParamAccess.tree);
            pManager.AddLineParameter("Lines", "L", "Lattice wireframe", GH_ParamAccess.list);
        }

        /// This is the method that actually does the work.
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables and assign initial invalid data.
            //    This way, if the input parameters fail to supply valid data, we know when to abort.
            double CellSize = 0;
            double xNum = 0;
            double yNum = 0;
            double zNum = 0;

            // 2. Retrieve input data.
            if (!DA.GetData(0, ref CellSize)) { return; }
            if (!DA.GetData(1, ref xNum)) { return; }
            if (!DA.GetData(2, ref yNum)) { return; }
            if (!DA.GetData(3, ref zNum)) { return; }

            // 3. If data is invalid, we need to abort.
            if (CellSize == 0) { return; }
            if (xNum == 0) { return; }
            if (yNum == 0) { return; }
            if (zNum == 0) { return; }

            //// 4. Let's do some real stuffz
            // Declare gh_structure data tree
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();

            // Here we declare the basepoint & position vectors
            Plane plane = Plane.WorldXY;
            Point3d BasePoint = plane.Origin;
            Vector3d Xdir = plane.XAxis * CellSize;
            Vector3d Ydir = plane.YAxis * CellSize;
            Vector3d Zdir = plane.ZAxis * CellSize;

            // Create grid of points (in data tree structure)
            for (int i = 0; i <= xNum; i++)
            {
                for (int j = 0; j <= yNum; j++)
                {
                    for (int k = 0; k <= zNum; k++)
                    {
                        Vector3d V1 = Xdir * i;
                        Vector3d V2 = Ydir * j;
                        Vector3d V3 = Zdir * k;

                        Point3d NewPt = BasePoint + V1 + V2 + V3;

                        // Construct path in the tree (custom data structure)
                        GH_Path pth = new GH_Path(0, k, i);

                        GridTree.Append(new GH_Point(NewPt), pth);
                    }
                }
            }

            // Create struts (GRID TOPOLOGY)
            // Kindof hack-y solution, could be much more elegant

            List<GH_Line> Struts = new List<GH_Line>();

            for (int i = 0; i <= xNum; i++)
            {
                for (int j = 0; j <= yNum; j++)
                {
                    for (int k = 0; k <= zNum; k++)
                    {
                        if (i < xNum)
                        {
                            GH_Path pth1 = new GH_Path(0, k, i);
                            GH_Path pth2 = new GH_Path(0, k, i + 1);
                            Point3d node1 = GridTree[pth1][j].Value;
                            Point3d node2 = GridTree[pth2][j].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                        if (j < yNum)
                        {
                            GH_Path pth1 = new GH_Path(0, k, i);
                            GH_Path pth2 = new GH_Path(0, k, i);
                            Point3d node1 = GridTree[pth1][j].Value;
                            Point3d node2 = GridTree[pth2][j + 1].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                        if (k < zNum)
                        {
                            GH_Path pth1 = new GH_Path(0, k, i);
                            GH_Path pth2 = new GH_Path(0, k + 1, i);
                            Point3d node1 = GridTree[pth1][j].Value;
                            Point3d node2 = GridTree[pth2][j].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                    }
                }
            }

            // Set output
            DA.SetDataTree(0, GridTree);
            DA.SetDataList(1, Struts);
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
            get { return new Guid("{6b57d79f-7ae9-4522-a30f-a3d8caaebfe6}"); }
        }
    }
}
