using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using IntraLattice.CORE.Data.GH_Goo;
using IntraLattice.CORE.Data;

namespace IntraLattice.CORE.Components.Cell
{
    public class LatticeCellGooComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the LatticeCellGoo class.
        /// </summary>
        public LatticeCellGooComponent()
            : base("LatticeCellGoo", "LattiCell",
                "Lattice Cell data structure",
                "IntraLattice2", "Cell")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "Curve", "list of Curve", GH_ParamAccess.list);
            pManager[0].Optional = false;
            pManager.AddNumberParameter("Segment Length", "s", "the minimal length of the polygonal segments", GH_ParamAccess.item, 0.25);
            pManager[1].Optional = true;
            pManager.AddNumberParameter("max. Deviation", "d", "maximal deviation of the Line approximation to the original curve", GH_ParamAccess.item, GH_Component.DocumentTolerance());
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("latticeCell", "LaCell", "Lattice Cell representation in form of data structure", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Line item = default(Line);
            PolylineCurve polylineCurve = new PolylineCurve();

            List<Line> listofline = new List<Line>();

            List<Curve> ListCurve = new List<Curve>();
            double SegLength = 0.01;
            double maxDeviation = 0.25;


            if (!(DA.GetDataList(0, ListCurve))) { return; }
            if (!(DA.GetData(1, ref  SegLength))) { return; }
            if (!(DA.GetData(2, ref  maxDeviation))) { return; }

            if (ListCurve == null && ListCurve.Count == 0) { return; }


            foreach (var element in ListCurve)
            {
                if (element.IsValid)
                {
                    if (element.IsLinear())
                    {
                        listofline.Add(new Line(element.PointAtStart, element.PointAtEnd));
                    }
                    else
                    {
                        if (!element.IsPolyline())
                        {
                            item = new Line(element.PointAtStart, element.PointAtEnd);
                            polylineCurve = element.ToPolyline(0, 0, Math.PI, 0.1, 0.0, SegLength, maxDeviation, item.Length, true);
                        }
                        else
                        {
                            polylineCurve = (PolylineCurve)element;
                        }
                        if (polylineCurve.PointCount > 0)
                        {
                            for (int i = 1; i < polylineCurve.PointCount; i++)
                            {
                                item = new Line(polylineCurve.Point(i - 1), polylineCurve.Point(i));
                                listofline.Add(item);
                            }
                        }
                    }
                }
            }

            var lattice = new LatticeCellGoo(new LatticeCell(listofline));

            DA.SetData(0, lattice);

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
            get { return new Guid("{0d4a7a79-c562-4479-ae55-1f96ca879320}"); }
        }
    }
}