using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FEA_Interface.Component
{
    public class MaterialProperty : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MaterialProperty class.
        /// Author: Yunlong Tang copyright reserved
        /// Data 9/10/2015
        /// </summary>
        public MaterialProperty()
            : base("Material_Property_Iso", "M_Iso",
                "Input material properties for FEA analysis, using linear elastic isotropic material model",
                "FEA_Interface", "Material Properties")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Young Modulus","E","Young modulus of input material",GH_ParamAccess.item);
            pManager.AddNumberParameter("Possion Ratio", "rmd", "Possion ratio of input material", GH_ParamAccess.item);
            pManager.AddNumberParameter("Density", "Density", "Density of input material", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Strut_ID", "S_ID", "The ID of strut", GH_ParamAccess.item); // if ID is smaller than zero it means this properties will be applied to all struts
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Material Properties","M_P","Material Properties",GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Read the input data
            double E = 0;
            double rmd = 0;
            double density = 0;
            int strutID = 0;
            if (!DA.GetData(0, ref E)) { return; }
            if (!DA.GetData(1, ref rmd)) { return; }
            if (!DA.GetData(2, ref density)) { return; }
            if (!DA.GetData(3, ref strutID)) { return; }

            string OString = strutID.ToString();
            OString += ",";
            OString += E.ToString();
            OString += ",";
            OString += rmd.ToString();
            OString += ",";
            OString += density.ToString();
            DA.SetData(0, OString);
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
                return Resource1.Material.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{A2A45DAD-A733-4312-B236-9C331B46C9C3}"); }
        }
    }
}