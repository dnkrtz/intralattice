using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

namespace IntraLattice._2___FRAME
{
    public class CustomCell : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the CustomCell class.
        /// </summary>
        public CustomCell()
            : base("CustomCell", "Nickname",
                "Description",
                "IntraLattice2", "Frame")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("(u,v,w) -> (u+1,v,w)", "(u,v,w) -> (u+1,v,w)", "(u,v,w) -> (u+1,v,w)", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("(u,v,w) -> (u,v+1,w)", "(u,v,w) -> (u,v+1,w)", "(u,v,w) -> (u,v+1,w))", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("(u,v,w) -> (u+1,v,w)", "(u,v,w) -> (u,v,w+1))", "(u,v,w) -> (u+1,v,w)", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "L", "Strut lines", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            //instantiate  new value list
            var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
            vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.Cycle;
            vallist.CreateAttributes();

            //customise value list position
            int inputcount = this.Component.Params.Input[1].SourceCount;
            vallist.Attributes.Pivot = new PointF((float)Component.Params.Input[1].Attributes.Bounds.X, (float)Component.Params.Input[1].Attributes.Bounds.Y);

            //populate value list with our own data
            vallist.ListItems.Clear();
            var item1 = new Grasshopper.Kernel.Special.GH_ValueListItem("Simple cubic", "0");
            var item2 = new Grasshopper.Kernel.Special.GH_ValueListItem("X-cross", "1");
            var item3 = new Grasshopper.Kernel.Special.GH_ValueListItem("Star", "2");
            var item4 = new Grasshopper.Kernel.Special.GH_ValueListItem("Star2", "3");
            vallist.ListItems.Add(item1);
            vallist.ListItems.Add(item2);
            vallist.ListItems.Add(item3);
            vallist.ListItems.Add(item4);

            //Until now, the slider is a hypothetical object.
            // This command makes it 'real' and adds it to the canvas.
            GrasshopperDocument.AddObject(vallist, false);

            //Connect the new slider to this component
            Component.Params.Input[1].AddSource(vallist);
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