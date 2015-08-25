using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using IntraLattice.CORE.Helpers;

// Summary:     This component cleans a curve network.
// ===============================================================================
// Details:     - 
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components.Utility
{
    public class CleanNetworkComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CleanNetworkComponent class.
        /// </summary>
        public CleanNetworkComponent()
            : base("Clean Network", "CleanNetwork",
                "Removes duplicate curves from a network, within specified tolerance.",
                "IntraLattice2", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Strut network to clean.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "Tol", "Tolerance for combining nodes.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Cleaned strut network.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables
            var struts = new List<Curve>();
            double tol = 0.0;
            // 2. Attempt to retrieve input
            if (!DA.GetDataList(0, struts)) { return; }
            if (!DA.GetData(1, ref tol)) { return; }
            // 3. Validate input
            if (struts == null || struts.Count == 1) { return; }
            if (tol < 0) { return; }

            // 4. Call cleaning method
            struts = FrameTools.CleanNetwork(struts, tol);

            // 5. Set output
            DA.SetDataList(0, struts);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// Icons need to be 24x24 pixels.
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
            get { return new Guid("{8b3a2f8c-3a76-4b19-84b9-f3eea80010ea}"); }
        }
    }
}