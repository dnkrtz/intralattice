using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace FEA_Interface.Component
{
    public class PointLoadComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public PointLoadComponent()
            : base("PointLoad", "PLoad",
                "Generate Load script of FEA analysis",
                "FEA_Interface", "Load")
        {

        }


        Vector3d m_LoadVector;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("LoadNodeIndex", "NodeIndex", "Node index of load", GH_ParamAccess.list);
            pManager.AddVectorParameter("ForceVector", "ForceV", "Magnitude and direction of force", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("LoadScript", "LOAD", "Load script for analysis", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Get Input
            List<int> nodeIndex = new List<int>();
            DA.GetDataList(0, nodeIndex);
            DA.GetData(1, ref m_LoadVector);
            List<string> OutputString = new List<string>();
            string sVector = m_LoadVector.ToString();
            for (int IoL = 0; IoL < nodeIndex.Count;IoL++)
            {
                string LoadString = "FORCE,1,";
                int NodeID = nodeIndex[IoL];
                LoadString = LoadString + NodeID.ToString();
                LoadString = LoadString + ",0,1.0,";
                LoadString = LoadString + sVector;
                OutputString.Add(LoadString);
            }
            DA.SetDataList(0,OutputString);

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
                return Resource1.Load.ToBitmap();
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{605F60A4-2EC1-4D59-82AC-B5AC05468F51}"); }
        }
    }
}
