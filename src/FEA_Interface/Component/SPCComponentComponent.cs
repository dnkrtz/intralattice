using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace FEA_Interface.Component
{
    public class SPCComponentComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SPCComponentComponent()
            : base("SPCComponent", "SPC",
                "Generate SPC for certain Node",
                "FEA_Interface", "Constraints")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("NodeIndex", "NodeIndex", "List Of Nodes Index", GH_ParamAccess.list);
            pManager.AddBooleanParameter("DoF_X", "X", "Translation Dof X", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("DoF_Y", "Y", "Translation Dof Y", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("DoF_Z", "Z", "Translation Dof Z", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("DoF_rX", "rX", "Rotation Dof rX", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("DoF_rY", "rY", "Rotation Dof rY", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("DoF_rZ", "rZ", "Rotation Dof rZ", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("SPCList","SPC","ListOfSPCConstraint",GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Get Parameters
            List<int> NodeIndex = new List<int>();
            bool tx = true;
            bool ty = true;
            bool tz = true;
            bool rx = true;
            bool ry = true;
            bool rz = true;
            if (!DA.GetDataList(0, NodeIndex)) return;
            if (!DA.GetData(1, ref tx)) return;
            if (!DA.GetData(2, ref ty)) return;
            if (!DA.GetData(3, ref tz)) return;
            if (!DA.GetData(4, ref rx)) return;
            if (!DA.GetData(5, ref ry)) return;
            if (!DA.GetData(6, ref rz)) return;
            List<string> oString = new List<string>();
            for (int IoC = 0; IoC < NodeIndex.Count; IoC++ )
            {
                string SPCstring = "SPC1,1,";
                if (tx)
                {
                    SPCstring = SPCstring + "1";
                }
                if (ty)
                {
                    SPCstring = SPCstring + "2";
                }
                if (tz)
                {
                    SPCstring = SPCstring + "3";
                }
                if (rx)
                {
                    SPCstring = SPCstring + "4";
                }
                if (ry)
                {
                    SPCstring = SPCstring + "5";
                }
                if (rz)
                {
                    SPCstring = SPCstring + "6";
                }
                SPCstring = SPCstring + ",";
                int NodePosition = NodeIndex[IoC];
                SPCstring = SPCstring + NodePosition.ToString();
                oString.Add(SPCstring);
            }
            DA.SetDataList(0, oString);

        }

        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return Resource1.Constraints.ToBitmap();
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{6B86901B-BD72-4F5B-BD78-E7B76AE3AAE6}"); }
        }
    }
}
