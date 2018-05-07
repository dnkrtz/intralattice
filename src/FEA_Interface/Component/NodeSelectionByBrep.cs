using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FEA_Interface.Component
{
    public class NodeSelectionByBrep : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NodeSelectionByBrep class.
        /// Author Yunlong
        /// Output node Index on selected Brep
        /// </summary>
        public NodeSelectionByBrep()
            : base("NodeSelectionByBrep", "NodeOnBrep",
                "Description",
                "FEA_Interface", "Tool")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("ListOfNodes","ListOfNodes","List of nodes position",GH_ParamAccess.list);
            pManager.AddBrepParameter("Brep", "Brep", "Input Brep", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tolerance", "Tol", "if point to the brep distance smaller than given value it can be regarded point is on the brep", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Node_Index","Node_Index","Index of nodes",GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Retrieve the input of components
            List<Point3d> NodePosition = new List<Point3d>();
            Brep pBrep = new Brep();
            double tol = 0;
            if (!DA.GetDataList(0, NodePosition)) { return; }
            if (!DA.GetData(1, ref pBrep)) { return; }
            if (!DA.GetData(2, ref tol)) { return; }

            List<int> ResultID = new List<int>();
            for (int IoP = 0; IoP < NodePosition.Count; IoP++)
            {
                Point3d CurrentPoint = NodePosition[IoP];
                if (IsPointOnBrep(pBrep, CurrentPoint,tol))
                {
                        ResultID.Add(IoP+1);
                }
            }
            DA.SetDataList(0, ResultID);

        }

        private bool IsPointOnBrep(Brep B, Point3d P, double t)
        {
            // Get Closest Point of Brep
            Point3d CP = B.ClosestPoint(P);

            // Distance of Closet Point and current point
            double d = P.DistanceTo(CP);
            if (d <= t)
            {
                return true;
            }
            else
            {
                return false;
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
                return Resource1.selectpointOnBrep.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{6E541D05-3F54-41F1-AE17-90DC38253061}"); }
        }
    }
}