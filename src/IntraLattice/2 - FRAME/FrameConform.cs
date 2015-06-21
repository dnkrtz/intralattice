using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// This component maps a unit cell topology to the lattice grid
// Assumption : Hexahedral cell lattice grid (i.e. morphed cubic cell)

namespace IntraLattice
{
    public class FrameConform : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public FrameConform()
            : base("FrameConform", "FrameConform",
                "Populates conformal grid with frame topology",
                "IntraLattice2", "Frame")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Topology", "Topo", "Unit cell topology\n0 - grid\n1 - x\n2 - star\n3 - star2\n4 - octa)", GH_ParamAccess.item, 0);
            pManager.AddPointParameter("Point Grid", "G", "Conformal lattice grid", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Lattice frame", "L", "Lattice list", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables and assign initial invalid data.
            int topo = 0;
            GH_Structure<GH_Point> gridTree = null;
            // Attempt to fetch data
            if (!DA.GetData(0, ref topo)) { return; }
            if (!DA.GetDataTree(1, out gridTree)) { return; }
            // Validate data
            if (gridTree == null) { return; }

            // Get size of the tree
            
            int[] index = gridTree.get_Path(gridTree.LongestPathIndex()).Indices;
            List<int> indx = new List<int>(index); // Cast to list

            // Initiate list of lattice lines
            List<GH_Line> struts = new List<GH_Line>();

            for (int i = 0; i <= indx[0]; i++)
            {
                for (int j = 0; j <= indx[1]; j++)
                {
                    for (int k = 0; k <= indx[2]; k++)
                    {

                        // We'll be needing the data tree path of the current node, and those of its neighbours
                        GH_Path currentPath = new GH_Path(i, j, k);
                        List<GH_Path> neighbourPaths = new List<GH_Path>();

                        // Get neighbours!!
                        FrameTools.TopologyNeighbours(ref neighbourPaths, topo, indx, i, j, k);

                        // Nere we create the actual struts
                        // Firt, make sure currentpath exists in the tree
                        if (gridTree.PathExists(currentPath))
                        {
                            // Connect current node to all its neighbours
                            Point3d pt1 = gridTree[currentPath][0].Value;
                            foreach (GH_Path NeighbourPath in neighbourPaths)
                            {
                                // Again, make sure the neighbourpath exists in the tree
                                if (gridTree.PathExists(NeighbourPath))
                                {
                                    Point3d pt2 = gridTree[NeighbourPath][0].Value;
                                    struts.Add(new GH_Line(new Line(pt1, pt2)));
                                }
                            }
                        }

                    }
                }
            }

            // Output data
            DA.SetDataList(0, struts);

        }

        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
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
            get { return new Guid("{c7b0a43c-e4f4-4484-8b5e-f9c8a501fd96}"); }
        }
    }
}