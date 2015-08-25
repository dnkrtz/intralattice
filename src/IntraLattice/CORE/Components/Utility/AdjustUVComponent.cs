using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

// Summary:     This component can be used to adjust the UV-Map of a surface, for alignment purposes.
// ===============================================================================
// Details:     - When using the uvw-conform components, the orientation of the surface UV-maps is important.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components.Utility
{
    public class AdjustUVComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the AdjustUVComponent class.
        /// </summary>
        public AdjustUVComponent()
            : base("Adjust UV", "AdjustUV",
                "Adjusts the UV-map of a surface for proper alignment with other surfaces/axes.",
                "Intralattice2", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "Surf", "Surface to adjust.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Swap UV", "SwapUV", "Swap the uv parameters.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reverse U", "ReverseU", "Reverse the u-parameter direction.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reverse V", "ReverseV", "Reverse the v-parameter direction.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Adjusted surface", "Surf", "Surface with adjusted uv-map.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate data
            Surface surf = null;
            bool swapUV = false;
            bool reverseU = false;
            bool reverseV = false;

            if (!DA.GetData(0, ref surf)) { return; }
            if (!DA.GetData(1, ref swapUV)) { return; }
            if (!DA.GetData(2, ref reverseU)) { return; }
            if (!DA.GetData(3, ref reverseV)) { return; }

            if (surf == null) { return; }

            // 2. Make adjustments, if specified.
            if (swapUV) surf = surf.Transpose();
            if (reverseU) surf.Reverse(0, true);
            if (reverseV) surf.Reverse(1, true);

            // 3. Set output
            DA.SetData(0, surf);
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
            get { return new Guid("{3372eac1-1545-4fca-9a25-72c4563aaa1f}"); }
        }
    }
}