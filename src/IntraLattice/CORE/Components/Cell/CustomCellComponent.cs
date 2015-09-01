using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using Rhino;
using IntraLattice.CORE.Data;
using IntraLattice.Properties;

// Summary:     This component processes/verifies user-defined unit cells, and outputs a valid unit cell
// ===============================================================================
// Details:     - Assumes unit cell is aligned with the xyz world axes.
//              - Checks validity of the unit cell.   
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components
{
    public class CustomCellComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CustomCellComponent class.
        /// </summary>
        public CustomCellComponent()
            : base("Custom Cell", "CustomCell",
                "Pre-processes a custom unit cell by check validity and outputting topology.",
                "IntraLattice", "Cell")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Custom Cell", "L", "Unit cell lines (curves must be linear).", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Topology", "Topo", "Verified unit cell topology", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve/validate input
            var curves = new List<Curve>();
            if (!DA.GetDataList(0, curves)) { return; }

            // 2. Convert curve input to line input
            var lines = new List<Line>();
            foreach (Curve curve in curves)
            {
                // Make sure the curve is linear, if not, return error and abort.
                if (!curve.IsLinear())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All struts must be linear.");
                    return;
                }
                // Convert curve to line
                lines.Add(new Line(curve.PointAtStart, curve.PointAtEnd));
            }
    
            // 3. Instantiate UnitCell object.
            UnitCell cell = new UnitCell(lines);
            
            // 4. CheckValidity instance method to check the unit cell. Use the return value to output useful error message.
            switch (cell.CheckValidity())
            {
                case -1:
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid cell - opposing faces must be identical.");
                    return;
                case 0:
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid cell - each face needs at least one node lying on it.");
                    return;
                case 1:
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Blank, "Your cell is valid!");
                    break;
            }

            // 5. Set output
            DA.SetData(0, cell);

        }

        /// <summary>
        /// Sets the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
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
                return Resources.customCell;
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