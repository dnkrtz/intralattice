using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Windows.Forms;

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
            pManager.AddVectorParameter("Derivatives", "D", "Directional derivatives of points", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Curvy", "Curvy", "Allow curved struts", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Bezier Factor", "BeziF", "Multiplication factor for bezier", GH_ParamAccess.item, 3);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Lattice frame", "L", "Lattice list", GH_ParamAccess.list);
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
            GH_Structure<GH_Vector> derivTree = null;
            bool curvy = true;
            double bFact = 0;

            // Attempt to fetch data
            if (!DA.GetData(0, ref topo)) { return; }
            if (!DA.GetDataTree(1, out gridTree)) { return; }
            if (!DA.GetDataTree(2, out derivTree)) { return; }
            if (!DA.GetData(3, ref curvy)) { return; }
            if (!DA.GetData(4, ref bFact)) { return; }
            // Validate data
            if (gridTree == null) { return; }
            if (derivTree == null) { return; }

            // Get size of the tree
            int[] index = gridTree.get_Path(gridTree.LongestPathIndex()).Indices;
            List<int> N = new List<int>(index); // Cast to list

            // Initiate list of lattice lines
            List<GH_Curve> struts = new List<GH_Curve>();

            for (int i = 0; i <= N[0]; i++)
            {
                for (int j = 0; j <= N[1]; j++)
                {
                    for (int k = 0; k <= N[2]; k++)
                    {

                        // We'll be needing the data tree path of the current node, and those of its neighbours
                        GH_Path currentPath = new GH_Path(i, j, k);
                        List<GH_Path> neighbourPaths = new List<GH_Path>();
                        List<int> neighbourDirections = new List<int>();

                        // Get neighbours!!
                        //FrameTools.TopologyNeighbours(ref neighbourPaths, topo, indx, i, j, k);
                        if (i < N[0])
                        {
                            neighbourPaths.Add(new GH_Path(i + 1, j, k));
                            neighbourDirections.Add(0);
                        }
                        if (j < N[1])
                        {
                            neighbourPaths.Add(new GH_Path(i, j + 1, k));
                            neighbourDirections.Add(1);
                        }
                        if (k < N[2])
                        {
                            neighbourPaths.Add(new GH_Path(i, j, k + 1));
                            neighbourDirections.Add(2);
                        }

                        // Nere we create the actual struts
                        // Firt, make sure currentpath exists in the tree
                        if (gridTree.PathExists(currentPath))
                        {
                            // Connect current node to all its neighbours
                            Point3d pt1 = gridTree[currentPath][0].Value;
                            
                            // currently only set up for CUBIC, need to fetch neighbour direction!!!!!!!!!!!!!
                            int testo = 0;

                            foreach (GH_Path neighbourPath in neighbourPaths)
                            {
                                // Again, make sure the neighbourpath exists in the tree
                                if (gridTree.PathExists(neighbourPath))
                                {
                                    Point3d pt2 = gridTree[neighbourPath][0].Value;

                                    // create the strut
                                    if (curvy && neighbourDirections[testo]<2)
                                    {
                                        List<Point3d> controlPoints = new List<Point3d>();
                                        controlPoints.Add(pt1);
                                        controlPoints.Add(pt1 + (derivTree[currentPath][neighbourDirections[testo]].Value)/(bFact*N[neighbourDirections[testo]]));
                                        controlPoints.Add(pt2 - (derivTree[neighbourPath][neighbourDirections[testo]].Value)/(bFact*N[neighbourDirections[testo]]));
                                        controlPoints.Add(pt2);
                                        BezierCurve curve = new BezierCurve(controlPoints);
                                            
                                        struts.Add(new GH_Curve(curve.ToNurbsCurve())); 
                                    }
                                    else
                                        struts.Add(new GH_Curve(new LineCurve(new Line(pt1, pt2))));
                                }
                                // increment direction ONLY WORKS FOR CUBIC, improve
                                testo+=1;
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
        /// Custom menu UI stuff
        /// </summary>
        /*public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            // need a way to refresh 'selection' parameter

            Menu_AppendItem(menu, "Cubic", Menu_TopoSelected, selection == 0, selection == 0);
            Menu_AppendItem(menu, "X", Menu_TopoSelected, selection == 1, selection == 1);
            Menu_AppendItem(menu, "Star", Menu_TopoSelected, selection == 2, selection == 2);
            Menu_AppendItem(menu, "Star2", Menu_TopoSelected, selection == 3, selection == 3);

            menu.
        }*/




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