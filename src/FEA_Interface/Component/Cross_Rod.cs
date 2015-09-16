using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FEA_Interface.Component
{
    public class Cross_Rod : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Cross_Rod class.
        /// Author: Yunlong Tang 
        /// This component is used to generate crossection of rod struts. This cross section parameter can be used as the input for the FEA interface
        /// </summary>
        public Cross_Rod()
            : base("Cross_Rod", "C_Rode",
                "Generate Rod Cross section for FEAInterface",
                "FEA_Interface", "Cross Section")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Start_Radius","S_R","Start Radius of struts",GH_ParamAccess.item);
            pManager.AddNumberParameter("End_Radius","E_R","End Radius of struts",GH_ParamAccess.item);
            pManager.AddIntegerParameter("Crossection_ID", "C_ID", "CrossectionID", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Material_ID", "M_ID", "Crossection related material", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Crossection_Properties", "C_P", "Properties of crossection", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // Initialize parameters
            double startRadius = -1;
            double endRadius = -1;
            int crossIndex = -1;
            int MatID = -1;
            if (!DA.GetData(0, ref startRadius)) { return; }
            if (!DA.GetData(1, ref endRadius)) { return; }
            if (!DA.GetData(2, ref crossIndex)) { return; }
            if (!DA.GetData(3, ref MatID)) {return;}

            // Add crossection type
            string crossString = "ROD";
            crossString = crossString +",";
            crossString = crossString + crossIndex.ToString();
            crossString = crossString +",";
            crossString = crossString + startRadius.ToString();
            crossString = crossString + ",";
            crossString = crossString + endRadius.ToString();
            crossString = crossString + ",";
            crossString = crossString + MatID.ToString();
            DA.SetData(0, crossString);
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
                return Resource1.Rod.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{0677DA07-A071-4799-882D-F2F4FDB29F8C}"); }
        }
    }
}