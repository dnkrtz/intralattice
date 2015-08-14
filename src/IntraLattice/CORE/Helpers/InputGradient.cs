using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using IntraLattice.Properties;
using System.Drawing;
using Grasshopper.Kernel.Expressions;

// This component is a post-processing tool used to inspect a mesh
// ===============================================================
// Checks that the mesh represents a solid

namespace IntraLattice.CORE.Helpers
{
    public class InputGradient : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the InputGradient class.
        /// </summary>
        public InputGradient()
            : base("Select Gradient", "Gradient",
                "Generates gradient string (i.e. a spatial math expression)",
                "IntraLattice2", "Mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Gradient Type", "Type", "Selection of gradient types", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Gradient String", "Grad", "The spatial gradient as an expression string", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 0. Setup input
            Component = this;
            GrasshopperDocument = this.OnPingDocument();
            //    Generate default input menu
            if (Component.Params.Input[0].SourceCount == 0) InputTools.GradientSelect(ref Component, ref GrasshopperDocument, 0, 11);

            // 1. Retrieve input
            int gradientType = 0;
            if (!DA.GetData(0, ref gradientType)) { return; }

            // 2. Initialize 
            string mathString = null;

            // 3. Define gradients here
            // Assume unitized bounding box ( 0<x<1 , 0<y<1, 0<z<1), where radius values range from minRadius (mathString=0) to maxRadius (mathString=1)
            // Based on this assumption, the actual values are scaled to the size of the bounding box of the lattice
            switch (gradientType)
            {
                case 0:     // Linear (X)
                    mathString = "Abs(x)";
                    break;
                case 1:     // Linear (Y)
                    mathString = "Abs(y)";
                    break;
                case 2:     // Linear (Z)
                    mathString = "Abs(z)";
                    break;
                case 3:     // Centered (X)
                    mathString = "Abs(2*x-1)";
                    break;
                case 4:     // Centered (Y)
                    mathString = "Abs(2*y-1)";
                    break;
                case 5:     // Centered (Z)
                    mathString = "Abs(2*z-1)";
                    break;
                case 6:     // Cylindrical (X)
                    mathString = "Sqrt(Abs(2*y-1)^2 + Abs(2*z-1)^2)/Sqrt(2)";
                    break;
                case 7:     // Cylindrical (Y)
                    mathString = "Sqrt(Abs(2*x-1)^2 + Abs(2*z-1)^2)/Sqrt(2)";
                    break;
                case 8:     // Cylindrical (Z)
                    mathString = "Sqrt(Abs(2*x-1)^2 + Abs(2*y-1)^2)/Sqrt(2)";
                    break;
                case 9:     // Spherical
                    mathString = "Sqrt(Abs(2*x-1)^2 + Abs(2*y-1)^2 + Abs(2*z-1)^2)/Sqrt(3)";
                    break;
                // If you add a new gradient, don't forget to add it in the value list (GradientSelect method)
            }

            // Output report
            DA.SetData(0, mathString);

        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.elec;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{6a4e5dcf-5d72-49fc-a543-c2465b14eb86}"); }
        }
    }
}