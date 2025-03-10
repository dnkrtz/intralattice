using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FEA_Interface.Component
{
    public class NodeSelectionByPoint : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NodeSelectionByPoint class.
        /// Author: Yunlong Tang
        /// data 9/11/2015
        /// </summary>
        public NodeSelectionByPoint()
            : base("NodeSelectionByPoint", "SelectNode_Point",
                "Output select point's node index",
                "FEA_Interface", "Tool")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Input is the nodes list and point
            pManager.AddPointParameter("NodesList", "NodesList", "List of all nodes", GH_ParamAccess.list);
            pManager.AddPointParameter("PointPosition", "Point", "List of point position", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "Tolerance", "Tolerance to consider two point are the same", GH_ParamAccess.item, 0.001);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("NodeIndex","NodeIndex","The node index of selected point",GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize input
            List<Point3d> nodesList = new List<Point3d>();
            List<Point3d> pointsList = new List<Point3d>();
            if (!DA.GetDataList(0, nodesList)) { return; }
            if (!DA.GetDataList(1, pointsList)) { return; }
            double Tol = 0.001;
            if (!DA.GetData(2, ref Tol)) { return; }
            List<int> nodesIndex = new List<int>();
            for(int IoP = 0; IoP<pointsList.Count; IoP++)
            {
                for(int IoN = 0; IoN<nodesList.Count;IoN++)
                {
                    if(nodesList[IoN].DistanceTo(pointsList[IoP])<Tol)
                    {
                        nodesIndex.Add(IoN + 1);
                        break;
                    }
                }
            }
            DA.SetDataList(0, nodesIndex);
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
                return Resource1.SelectNodeByPoint.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{3F9C2517-768C-45CE-9735-A054DA16C600}"); }
        }
    }
}