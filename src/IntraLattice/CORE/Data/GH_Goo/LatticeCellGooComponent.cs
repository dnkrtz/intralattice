using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
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

    public class LatticeCellGooComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the LatticeCellGoo class.
        /// </summary>
        public LatticeCellGooComponent()
            : base("LatticeCellGoo", "Nickname",
                "Description",
                "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
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