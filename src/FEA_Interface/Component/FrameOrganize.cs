using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FEA_Interface.Component
{
    public class FrameOrganize : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FrameOrganize class.
        /// This component is written by Yunlong Tang and copyright reserved
        /// This component is used to organize frame and provide input for FEA interface
        /// </summary>
        public FrameOrganize()
            : base("FrameOrganize", "F_Organize",
                "Organize frame for Nastran component input",
                "FEA_Interface", "Tool")
        {

        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("L_Frame", "LF", "Lines list of lattice frame", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "T", "The node distance under this value is regarded as the same node", GH_ParamAccess.item);
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Node_Position","N_P","Position of Node",GH_ParamAccess.list);
            pManager.AddIntegerParameter("Start_Node", "S_Node", "Index of start node for each strut (begin from 0)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("End_Node", "E_Node", "Index of end node for each strut (begin from 0)", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Define INPUT the value
            List<Line> listOfStruts = new List<Line>();
            double Tol = 3e-3;

            // Define OUTPUT value
            List<Point3d> oListOfNodes = new List<Point3d>();
            List<int> oSNode = new List<int>();
            List<int> oENode = new List<int>();
            // Retrieve input
            if (!DA.GetDataList(0, listOfStruts)) { return; }
            if (!DA.GetData(1, ref Tol)) { return; }
            // Organize frame
            for (int IoL = 0; IoL < listOfStruts.Count; IoL++)
            {
                Line CLine = listOfStruts[IoL];
                Point3d StartP = CLine.From;
                Point3d EndP = CLine.To;
                int StartPosition = -1;
                int EndPosition = -1;
                // find exact the same node
                // Be careful node number and node position has 1 deference
                for (int IoN = 0; IoN < oListOfNodes.Count; IoN++)
                {
                    Point3d CNode = oListOfNodes[IoN];
                    if (StartP.DistanceTo(CNode) < Tol)
                    {
                        StartPosition = IoN;
                    }
                    if (EndP.DistanceTo(CNode) < Tol)
                    {
                        EndPosition = IoN;
                    }
                }
                if (StartPosition != -1)
                {
                     oSNode.Add(StartPosition + 1);
                }
                else
                {
                    oListOfNodes.Add(StartP);
                    oSNode.Add(oListOfNodes.Count);
                }
                if (EndPosition != -1)
                {
                    oENode.Add(EndPosition + 1);
                }
                else
                {
                    oListOfNodes.Add(EndP);
                    oENode.Add(oListOfNodes.Count);
                }

            }
            DA.SetDataList(0, oListOfNodes);
            DA.SetDataList(1, oSNode);
            DA.SetDataList(2, oENode);
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
                return Resource1.Organizer.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{DA39D7D0-5762-4AB0-8037-35DA3024FF2E}"); }
        }
    }
}