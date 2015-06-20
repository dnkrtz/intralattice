using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

// This component maps a unit cell topology to the lattice grid
// Also TRIMS the resulting lattice to the shape of the design space
// Assumption : Hexahedral cell lattice grid (i.e. morphed cubic cell)

namespace IntraLattice
{
    public class FrameUniform : GH_Component
    {
        public FrameUniform()
            : base("FrameUniform", "CMap",
                "Populates grid with lattice topology (and trims to design space)",
                "IntraLattice2", "Frame")
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

            // Get size of the tree
            // This works well for full grids ->       int[] indx = GridTree.get_Path(GridTree.LongestPathIndex()).Indices;
            // Since some grids are trimmed, we use a more robust approach
            List<int> indx = new List<int>{0,0,0};
            foreach (GH_Path Path in GridTree.Paths)
            {
                if ( Path.Indices[0] > indx[0] ) indx[0] = Path.Indices[0];
                if ( Path.Indices[1] > indx[1] ) indx[1] = Path.Indices[1];
                if ( Path.Indices[2] > indx[2] ) indx[2] = Path.Indices[2];
            }

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
                     
                        // Get neighbours!!
                        FrameTools.TopologyNeighbours(ref NeighbourPaths, Topo, indx, i, j, k);

                        // Nere we create the actual struts
                        // Firt, make sure currentpath exists in the tree
                        if (GridTree.PathExists(CurrentPath))
                        {
                            // Connect current node to all its neighbours
                            Point3d Node1 = GridTree[CurrentPath][0].Value;
                            foreach (GH_Path NeighbourPath in NeighbourPaths)
                            {
                                // Again, make sure the neighbourpath exists in the tree
                                if (GridTree.PathExists(NeighbourPath))
                                {
                                    Point3d Node2 = GridTree[NeighbourPath][0].Value;
                                    Struts.Add(new GH_Line(new Line(Node1, Node2)));
                                }
                            }
                        }
                        

                        //
                    }
                }
            }
      

            // Output grid
            DA.SetDataList(0, Struts);
        }

        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
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
