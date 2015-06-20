using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

// This component maps a unit cell topology to the lattice grid.
// Assumption : Hexahedral cell lattice grid (i.e. morphed cubic cell)

namespace IntraLattice
{
    public class CellMapper : GH_Component
    {
        public CellMapper()
            : base("CellMapper", "CMap",
                "Populates grid with lattice topology",
                "IntraLattice2", "Mapping")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Topology", "Topo", "Unit cell topology\n0 - grid\n1 - x\n2 - star\n3 - star2\n4 - octa)", GH_ParamAccess.item, 0);
            pManager.AddPointParameter("Point Grid", "G", "Conformal lattice grid", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Lattice frame", "L", "Lattice list", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables and assign initial invalid data.
            int Topo = 0;
            GH_Structure<GH_Point> GridTree = null;

            // Attempt to fetch data
            if (!DA.GetData(0, ref Topo)) { return; }
            if (!DA.GetDataTree(1, out GridTree)) { return; }

            // Validate data
            if (GridTree == null) { return; }

            // Get size of the tree (FULL GRID ONLY, will not work for uniform trimmed grid)
            // For partial grids, a potential solution is to check every path
            int[] indx = GridTree.get_Path(GridTree.LongestPathIndex()).Indices;

            // Initiate list of lattice lines
            List<GH_Line> Struts = new List<GH_Line>();

            for (int i = 0; i <= indx[0]; i++)
            {
                for (int j = 0; j <= indx[1]; j++)
                {
                    for (int k = 0; k <= indx[2]; k++)
                    {
                        
                        // We'll be needing the data tree path of the current node, and those of its neighbours
                        GH_Path CurrentPath = new GH_Path(i,j,k);
                        List<GH_Path> NeighbourPaths = new List<GH_Path>();
                     
                        // Set neighbours

                        // grid
                        if ( Topo == 0 )
                        {
                            if (i<indx[0])                                          NeighbourPaths.Add(new GH_Path(i+1, j, k));
                            if (j<indx[1])                                          NeighbourPaths.Add(new GH_Path(i, j+1, k));
                            if (k<indx[2])                                          NeighbourPaths.Add(new GH_Path(i, j, k+1));
                        }
                        // x
                        else if ( Topo == 1 )
                        {
                            if ((i<indx[0]) && (j<indx[1]) && (k<indx[2]))          NeighbourPaths.Add(new GH_Path(i+1, j+1, k+1));
                            if ((i<indx[0]) && (j>0) && (k<indx[2]))                NeighbourPaths.Add(new GH_Path(i+1, j-1, k+1));
                            if ((i > 0) && (j > 0) && (k < indx[2]))                NeighbourPaths.Add(new GH_Path(i-1, j-1, k+1));
                            if ((i > 0) && (j < indx[1]) && (k < indx[2]))          NeighbourPaths.Add(new GH_Path(i-1, j+1, k+1));
                        }
                        // star
                        else if ( Topo == 2 )
                        {
                            if ((i < indx[0]) && (j < indx[1]) && (k < indx[2]))    NeighbourPaths.Add(new GH_Path(i+1, j+1, k+1));
                            if ((i < indx[0]) && (j > 0) && (k < indx[2]))          NeighbourPaths.Add(new GH_Path(i+1, j-1, k+1));
                            if ((i > 0) && (j > 0) && (k < indx[2]))                NeighbourPaths.Add(new GH_Path(i-1, j-1, k+1));
                            if ((i > 0) && (j < indx[1]) && (k < indx[2]))          NeighbourPaths.Add(new GH_Path(i-1, j+1, k+1));
                            if (i < indx[0])                                        NeighbourPaths.Add(new GH_Path(i+1, j, k));
                            if (j < indx[1])                                        NeighbourPaths.Add(new GH_Path(i, j+1, k));
                        }
                        // star2
                        else if ( Topo == 3 )
                        {
                            if ((i < indx[0]) && (j < indx[1]) && (k < indx[2]))    NeighbourPaths.Add(new GH_Path(i+1, j+1, k+1));
                            if ((i < indx[0]) && (j > 0) && (k < indx[2]))          NeighbourPaths.Add(new GH_Path(i+1, j-1, k+1));
                            if ((i > 0) && (j > 0) && (k < indx[2]))                NeighbourPaths.Add(new GH_Path(i-1, j-1, k+1));
                            if ((i > 0) && (j < indx[1]) && (k < indx[2]))          NeighbourPaths.Add(new GH_Path(i-1, j+1, k+1));
                            if (i < indx[0])                                        NeighbourPaths.Add(new GH_Path(i+1, j, k));
                            if (j < indx[1])                                        NeighbourPaths.Add(new GH_Path(i, j+1, k));
                            if (k < indx[2])                                        NeighbourPaths.Add(new GH_Path(i, j, k+1));
                        }
                        // octahedral
                        else if ( Topo == 4 )
                        {
                            
                        }

                        // Nere we create the actual struts
                        // Connect current node to all its neighbours
                        Point3d Node1 = GridTree[CurrentPath][0].Value;
                        foreach (GH_Path NeighbourPath in NeighbourPaths)
                        {
                            Point3d Node2 = GridTree[NeighbourPath][0].Value;
                            Struts.Add(new GH_Line(new Line(Node1, Node2)));
                        }

                        //
                    }
                }
            }
      

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
