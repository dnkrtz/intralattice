using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using Rhino;
using IntraLattice.CORE.Data;

// Summary:     This component processes/verifies user-defined unit cells, and outputs a valid Topo unit cell
// ===============================================================================
// Details:     - Assumes unit cell is aligned with the xyz world axes
//              - Begins by fixing any undefined intersections (intersections must be defined nodes)
//              - Checks validity of the unit cell (opposing faces must be identical, in terms of nodes, to ensure continuity)     
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components
{
    public class CustomCell : GH_Component
    {
        public CustomCell()
            : base("CustomCell", "CustomCell",
                "Pre-processes a custom unit cell by check validity and outputting topology.",
                "IntraLattice2", "Cell")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Custom Cell", "L", "Unit cell lines (curves must be linear).", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Topology", "Topo", "Verified unit cell topology", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve input
            var curves = new List<Curve>();
            if (!DA.GetDataList(0, curves)) { return; }

            // Convert curve input to line input
            var lines = new List<Line>();
            foreach (Curve curve in curves)
            {
                // Make sure the curve is linear, if not, abort and return error
                if (!curve.IsLinear())
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All struts must be linear.");
                    return;
                }
                // Convert curve to line
                lines.Add(new Line(curve.PointAtStart, curve.PointAtEnd));
            }
    
            LatticeCell cell = new LatticeCell();
            
            int validity = cell.CheckValidity();

            switch (validity)
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

            DA.SetDataList(0, lines);

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