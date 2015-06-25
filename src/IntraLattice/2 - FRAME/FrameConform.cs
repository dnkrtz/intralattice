using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Windows.Forms;

// This component maps a unit cell topology to the lattice grid
// ============================================================
// Populates the grid with a specified lattice toplogy.
// If Morph=true, the uv struts are bezier curves which use the derivative information to morph to the design space.
// It uses the TopologyNeighbours method to get node adjacency information.
// Generally, no duplicate struts are created.
// If derivtree is not input, we automatically set morph=false and generate linear struts (vs. morphed curves)

// Written by Aidan Kurtz (http://aidankurtz.com)

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
            pManager.AddPointParameter("Point Grid", "Grid", "Conformal lattice grid", GH_ParamAccess.tree);
            pManager.AddVectorParameter("Derivatives", "Derivs", "Directional derivatives of points", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Morph", "Morph", "If true, struts will morph to the design space (as bezier curves)", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Morph Factor", "MF", "Division factor for bezier vectors (recommended: 2.0-3.0)", GH_ParamAccess.item, 3);
            pManager[2].Optional = true; // deriv tree only required when morph == true
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
            // 1. Retrieve and validate data
            int topo = 0;
            double bFact = 0;
            bool morphed = true;
            GH_Structure<GH_Point> gridTree = null;
            GH_Structure<GH_Vector> derivTree = null;

            if (!DA.GetData(0, ref topo)) { return; }
            if (!DA.GetDataTree(1, out gridTree)) { return; }
            if (!DA.GetDataTree(2, out derivTree)) { return; }
            if (!DA.GetData(3, ref morphed)) { return; }
            if (!DA.GetData(4, ref bFact)) { return; }

            if (gridTree == null) { return; }
            if (derivTree.IsEmpty && morphed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Since the derivs input is missing, we cannot morph.");
                morphed = false;
            }

            // 2. Get size of the tree
            double[] N = new double[] { 0, 0, 0 };
            foreach (GH_Path path in gridTree.Paths)
            {
                if (path.Indices[0] > N[0]) N[0] = path.Indices[0];
                if (path.Indices[1] > N[1]) N[1] = path.Indices[1];
                if (path.Indices[2] > N[2]) N[2] = path.Indices[2];
            }

            // 3. Now we create lattice struts
            var struts = new List<GH_Curve>();
            //    Loop over all points in the grid
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {

                        // We'll be needing the data tree path of the current node, and those of its neighbours
                        GH_Path currentPath = new GH_Path(u, v, w);
                        List<GH_Path> neighbourPaths = new List<GH_Path>();

                        // Get neighbours!!
                        FrameTools.TopologyNeighbours(ref neighbourPaths, topo, N, u, v, w);

                        // Nere we create the actual struts
                        // Firt, make sure currentpath exists in the tree
                        if (gridTree.PathExists(currentPath))
                        {
                            // Connect current node to all its neighbours
                            Point3d pt1 = gridTree[currentPath][0].Value;   // current node

                            // Cycle through all neighbours
                            foreach (GH_Path neighbourPath in neighbourPaths)
                            {
                                // Make sure the neighbourpath exists in the tree
                                if (gridTree.PathExists(neighbourPath))
                                {
                                    Point3d pt2 = gridTree[neighbourPath][0].Value; // neighbour node
                                    
                                    // Create the strut
                                    // If 'curved' is true, we create a bezier curve based on the uv derivatives (unless dealing with neighbours in w-direction)
                                    if (morphed && currentPath.Indices[2]==neighbourPath.Indices[2])
                                    {
                                        List<Vector3d> du = new List<Vector3d>();
                                        List<Vector3d> dv = new List<Vector3d>();
                                        List<Vector3d> duv = new List<Vector3d>();

                                        // If neighbours in u-direction
                                        if (neighbourPath.Indices[0] != currentPath.Indices[0])
                                        {
                                            // Notice bFact here, it scales the vector to give a better morphing
                                            du.Add( derivTree[currentPath][0].Value / (bFact*N[0]) );   // du[0] is the current node derivative
                                            du.Add( derivTree[neighbourPath][0].Value / (bFact*N[0]) ); // du[1] is the neighbour node derivative
                                        }
                                        // If neighbours in v-direction
                                        if (neighbourPath.Indices[1] != currentPath.Indices[1])
                                        {
                                            dv.Add( derivTree[currentPath][1].Value / (bFact*N[1]) );   // dv[0] is the current node derivative
                                            dv.Add( derivTree[neighbourPath][1].Value / (bFact*N[1]) ); // dv[1] is the neighbour node derivative
                                        }
                                        // If neighbours in uv-direction (diagonal)
                                        if ( du.Count == 2 && dv.Count == 2)
                                        {
                                            // Compute cross-derivative
                                            duv.Add((du[0] + dv[0]) / (Math.Sqrt(2)));  // duv[0] is the current node derivative
                                            duv.Add((du[1] + dv[1]) / (Math.Sqrt(2)));  // duv[1] is the neighbour node derivative
                                        }

                                        // Now we have everything we need to build a bezier curve
                                        List<Point3d> controlPoints = new List<Point3d>();
                                        controlPoints.Add(pt1); // first control point (vertex)
                                        if ( duv.Count == 2 )
                                        {
                                            controlPoints.Add(pt1 + duv[0]);
                                            controlPoints.Add(pt2 - duv[1]);
                                        }
                                        else if (du.Count == 2)
                                        {
                                            controlPoints.Add(pt1 + du[0]);
                                            controlPoints.Add(pt2 - du[1]);
                                        }
                                        else if (dv.Count == 2)
                                        {
                                            controlPoints.Add(pt1 + dv[0]);
                                            controlPoints.Add(pt2 - dv[1]);
                                        }
                                        controlPoints.Add(pt2); // fourth control point (vertex)
                                        BezierCurve curve = new BezierCurve(controlPoints);
                                            
                                        struts.Add(new GH_Curve(curve.ToNurbsCurve())); 
                                    }
                                    // If not 'curved', or in w-direction, create a simple linear strut
                                    else
                                        struts.Add(new GH_Curve(new LineCurve(new Line(pt1, pt2))));
                                }
                            }
                        }

                    }
                }
            }

            // 4. Set output
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