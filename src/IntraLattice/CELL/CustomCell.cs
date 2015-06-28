using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

namespace IntraLattice
{
    public class CustomCell : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        public CustomCell()
            : base("CustomCell", "Nickname",
                "Description",
                "IntraLattice2", "Cell")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("just", "a", "test", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("just", "a", "test", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("just", "a", "test", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();
            InputTools.TopoSelect(ref Component, ref GrasshopperDocument, 0);
            InputTools.BooleanSelect(ref Component, ref GrasshopperDocument, 1);
        }

        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.tertiary;
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{93998286-27d4-40a3-8f0e-043de932b931}"); }
        }
    }
}