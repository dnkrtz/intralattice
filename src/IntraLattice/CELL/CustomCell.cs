using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

namespace IntraLattice
{
    public class CustomCell : GH_Component
    {
        public CustomCell()
            : base("CustomCell", "CustomCell",
                "Description",
                "IntraLattice2", "Cell")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Custom ", "L", "test", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Topology", "Topo", "Line topology", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve input
            var lines = new List<LineCurve>();
            if (!DA.GetDataList(0, lines)) { return; }

            // Check 1 - Check that all struts are lines, and unitize their parameter domain
            BoundingBox bound = new BoundingBox();
            foreach (LineCurve line in lines)
            {
                line.Domain = new Interval(0, 1); // unitize parameter domain
                bound.Union(line.GetBoundingBox(true)); // combine bounding box to full cell box
                if (!line.IsLinear()) return; // if not linear, return
            }

            // Check 2 - Opposing faces must be identical
            Plane[] xy = new Plane[2];
            xy[0] = new Plane(bound.Corner(true, true, true), Plane.WorldXY.ZAxis);
            xy[1] = new Plane(bound.Corner(true, true, false), Plane.WorldXY.ZAxis);
            Plane[] yz = new Plane[2];
            yz[0] = new Plane(bound.Corner(true, true, true), Plane.WorldXY.XAxis);
            yz[1] = new Plane(bound.Corner(false, true, true), Plane.WorldXY.XAxis);
            Plane[] zx = new Plane[2];
            zx[0] = new Plane(bound.Corner(true, true, true), Plane.WorldXY.YAxis);
            zx[1] = new Plane(bound.Corner(true, false, true), Plane.WorldXY.YAxis);
            // WORK IN PROGRESS
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