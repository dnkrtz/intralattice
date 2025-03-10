using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FEA_Interface.Component
{
    public class StrutsCrossectionID : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the StrutsCrossectionID class.
        /// This component is used to describe the relationship between struts and crossection
        /// Author Yunlong and Copyright reserved
        /// </summary>
        public StrutsCrossectionID()
            : base("LinkStrutToCross", "LinkSToC",
                "To indicate the strut's crossssection by ID",
                "FEA_Interface", "Tool")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("StrutID", "S_ID", "ID of strut", GH_ParamAccess.item);
            pManager.AddIntegerParameter("CrossectionID", "C_ID", "ID of cross section", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Strut-Cross","S-C","strut cross section relationship",GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Read data
            int strutID = 0;
            int crossID = 0;
            if (!DA.GetData(0, ref strutID)) { return; }
            if (!DA.GetData(1, ref crossID)) { return; }
            string oString = strutID.ToString();
            oString += ",";
            oString += crossID.ToString();
            DA.SetData(0, oString);
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
                return Resource1.CrossToStruts.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{0EB55393-A684-499F-9685-66903A661294}"); }
        }
    }
}