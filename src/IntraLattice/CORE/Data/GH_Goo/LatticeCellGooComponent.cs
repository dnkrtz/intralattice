using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace IntraLattice.CORE.Data.GH_Goo
{
    public class LatticeCellGoo : Grasshopper.Kernel.Types.GH_GeometricGoo<LatticeCell>, IGH_PreviewData
    {
        //constructor
        public LatticeCellGoo()
        {
            this.Value = new LatticeCell();
        }

        public LatticeCellGoo(List<Rhino.Geometry.Line> rawcell) 
        {
            this.Value = new LatticeCell(rawcell);
        
        }

        public LatticeCellGoo(LatticeCell cell)
        {
            if (cell == null)
            {
                cell = new LatticeCell();
            }
            this.Value = cell.Duplicate();
        }

        public override Grasshopper.Kernel.Types.IGH_GeometricGoo DuplicateGeometry()
        {
            return Duplicate();
        }

        public LatticeCellGoo Duplicate()
        {
            return new LatticeCellGoo(Value == null ? new LatticeCell() : Value.Duplicate());
        }

        public override bool IsValid
        {
            get
            {
                if (Value == null) { return false; }
                return base.IsValid;
            }
        }

        public override string IsValidWhyNot
        {
            get
            {
                //add more info
                if (Value.Nodes == null) { return "Node list empty"; }
                return base.IsValidWhyNot;
            }
        }

        public override string ToString()
        {
            if (Value == null)
                return "Null LatticeCell";
            else
                return Value.ToString();
        }

        public override string TypeDescription
        {
            get { return ("LatticeCell Representation"); }
        }

        public override BoundingBox Boundingbox
        {
            get
            {
                if (Value == null) { return BoundingBox.Empty; }
                if (Value.Nodes == null) { return BoundingBox.Empty; }
                return Value.Nodes.BoundingBox;
            }
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            if (Value == null) { return BoundingBox.Empty; }
            if (Value.Nodes == null) { return BoundingBox.Empty; }
            return Value.Nodes.BoundingBox;
        }

        public override Grasshopper.Kernel.Types.IGH_GeometricGoo Transform(Transform xform)
        {
            if (Value == null) { return null; }
            if (Value.Nodes == null) { return null; }
            this.m_value.Nodes.Transform(xform);
            return this;
        }

        public override Grasshopper.Kernel.Types.IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            for (int i = 0 ; i < this.Value.Nodes.Count; i++) 
            {
                this.Value.Nodes[i] = xmorph.MorphPoint(this.Value.Nodes[i]);
            }

            return this;
        }

        public BoundingBox ClippingBox
        {
            get { return Boundingbox; }
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Value == null) { return; }
            else
            {
                foreach (var element in this.Value.Nodes) 
                {
                    args.Pipeline.DrawPoint(element, args.Color);
                }                
            }
     
        }

    }


    public class LatticeCellParameter : GH_PersistentGeometryParam<LatticeCellGoo>, IGH_PreviewObject 
    {
        public LatticeCellParameter() : base(new GH_InstanceDescription("LatticeCell","cell","Lattice cell data","Intralattice","Data")) 
        { 
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{4bd10f65-fe3e-48e0-a6be-745c32ee3351}"); }
        }
        public BoundingBox ClippingBox
        {
            get
            {
                return Preview_ComputeClippingBox();
            }
        }
        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            //Meshes aren't drawn.
        }
        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            //Use a standard method to draw gunk, you don't have to specifically implement this.
            Preview_DrawWires(args);
        }

        private bool m_hidden = false;
        public bool Hidden
        {
            get { return m_hidden; }
            set { m_hidden = value; }
        }
        public bool IsPreviewCapable
        {
            get { return true; }
        }
    
    }


    public class LatticeCellGooComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the LatticeCellGoo class.
        /// </summary>
        public LatticeCellGooComponent()
            : base("LatticeCellGoo", "LattiCell",
                "Lattice Cell data structure",
                "IntraLattice", "Data")
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
            pManager.AddParameter(new LatticeCellParameter(),"LatticeCell", "LC","data rep of LatticeCell",GH_ParamAccess.item);
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

            //polygonize curve
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

            var lattice = new LatticeCellGoo();

            foreach (var element in listofline) 
            {
                

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
            get { return new Guid("{0d4a7a79-c562-4479-ae55-1f96ca879320}"); }
        }
    }
}