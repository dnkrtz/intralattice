using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FEA_Interface.Component
{
    public class StrutSelectionByLine : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the StrutSelectionByLine class.
        /// Author:Yunlong Tang
        /// This component is used to select struts by lines
        /// </summary>
        public StrutSelectionByLine()
            : base("StrutSelectionByLine", "Strut_Line",
                "Select struts by line",
                "FEA_Interface", "Tool")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("ListOfAllStruts", "ListOfAllStruts", "all the line in the lattice frame", GH_ParamAccess.list);
            pManager.AddLineParameter("SelectedLines", "Line", "selected lines", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("StrutIndex", "StrutIndex", "Index of selected line", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Read initial value
            List<Line> listOfStruts = new List<Line>();
            List<Line> listOfLine = new List<Line>();
            List<int> listofIndex = new List<int>();
            if (!DA.GetDataList(0, listOfStruts)) { return; }
            if (!DA.GetDataList(1, listOfLine)) { return; }

            for(int ioL = 0; ioL<listOfLine.Count; ioL++)
            {
                for(int ioS = 0; ioS<listOfStruts.Count; ioS++)
                {
                    if(listOfLine[ioL].Equals(listOfStruts[ioS]))
                    {
                        listofIndex.Add(ioS + 1);
                        break;
                    }
                }
            }
            DA.SetDataList(0, listofIndex);
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
                return Resource1.SelectPointbyLine.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{45254F20-8A01-4923-8C5A-FBBB0FF3EF83}"); }
        }
    }
}