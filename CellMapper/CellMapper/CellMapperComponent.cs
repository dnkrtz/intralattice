using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component maps a unit cell topology to the lattice grid.
// Assumption : Hexahedral cell lattice grid (i.e. morphed cubic cell)

namespace CellMapper
{
    public class CellMapperComponent : GH_Component
    {
        public CellMapperComponent()
            : base("CellMapper", "CMap",
                "Populates grid with lattice topology",
                "IntraLattice2", "Mapping")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Topology", "C", "Cell topology (0 - none, 1 - Grid)", GH_ParamAccess.tree);
            pManager.AddPointParameter("Point Grid", "G", "Conformal lattice grid", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Lattice frame", "L", "Lattice list", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables and assign initial invalid data.
            int C = 0;
            GH_Structure<GH_Point> GridTree = null;

            // Attempt to fetch data
            if (!DA.GetData(0, ref C)) { return; }
            if (!DA.GetDataTree(1, out GridTree)) { return; }

            // Validate data
            if (C == 0) { return; }
            if (GridTree == null) { return; }

            // Initiate list of lattice lines
            List<GH_Line> Struts = new List<GH_Line>();

            
            /* OLD VERSION, NOW WE DON'T HAVE tNum, zNum, rNum. IMPROVE THIS
            for (int i = 0; i <= tNum; i++)
            {
                for (int j = 0; j <= zNum; j++)
                {
                    for (int k = 0; k <= rNum; k++)
                    {
                        if (i < tNum)
                        {
                            GH_Path pth1 = new GH_Path(0, i, j);
                            GH_Path pth2 = new GH_Path(0, i + 1, j);
                            Point3d node1 = GridTree[pth1][k].Value;
                            Point3d node2 = GridTree[pth2][k].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                        if (j < rNum)
                        {
                            GH_Path pth1 = new GH_Path(0, j, i);
                            GH_Path pth2 = new GH_Path(0, j, i);
                            Point3d node1 = GridTree[pth1][k].Value;
                            Point3d node2 = GridTree[pth2][k + 1].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                        if (j < zNum)
                        {
                            GH_Path pth1 = new GH_Path(0, j, i);
                            GH_Path pth2 = new GH_Path(0, j + 1, i);
                            Point3d node1 = GridTree[pth1][k].Value;
                            Point3d node2 = GridTree[pth2][k].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                    }
                }
            }
            */

            // Output grid
            DA.SetDataList(0, Struts);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{c60d6bd4-083b-4b54-b840-978d251d9653}"); }
        }
    }
}
