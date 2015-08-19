using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace IntraLattice.CORE.Data.GH_Goo
{
    public class LatticeCellGoo : Grasshopper.Kernel.Types.GH_GeometricGoo<LatticeCell>, IGH_PreviewData
    {
        #region Constructors
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
            this.Value = cell;
        }
        public override Grasshopper.Kernel.Types.IGH_GeometricGoo DuplicateGeometry()
        {
            return DuplicateGoo();
        }
        public LatticeCellGoo DuplicateGoo()
        {
            return new LatticeCellGoo(Value == null ? new LatticeCell() : Value.Duplicate());
        }
        #endregion

        #region Properties
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
                if (Value.NodeNeighbours == null) { return "single Point"; }
                if (Value.NodePairs == null) { return "No line"; }
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

        public override string TypeName
        {
            get { return "LatticeCellGoo"; }
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
        #endregion

        #region Casting Methods
        public override bool CastTo<Q>(out Q target)
        {
            //Cast to LatticeCell.
            if (typeof(Q).IsAssignableFrom(typeof(LatticeCell)))
            {
                if (Value == null)
                    target = default(Q);
                else
                    target = (Q)(object)Value;
                return true;
            }
            target = default(Q);
            return false;
        }
        public override bool CastFrom(object source)
        {
            if (source == null) { return false; }

            //Cast from LatticeCell
            if (typeof(LatticeCell).IsAssignableFrom(source.GetType()))
            {
                Value = (LatticeCell)source;
                return true;
            }

            return false;
        }
        #endregion

        #region Transformation Methods
        // no idea if they work
        public override Grasshopper.Kernel.Types.IGH_GeometricGoo Transform(Transform xform)
        {
            if (Value == null) { return null; }
            if (Value.Nodes == null) { return null; }
            this.m_value.Nodes.Transform(xform);
            return this;
        }
        //no idea if they work
        public override Grasshopper.Kernel.Types.IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            for (int i = 0 ; i < this.Value.Nodes.Count; i++) 
            {
                this.Value.Nodes[i] = xmorph.MorphPoint(this.Value.Nodes[i]);
            }

            return this;
        }
        #endregion

        #region Drawing Methods
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

                foreach (var element in this.Value.NodePairs) 
                {
                    
                    args.Pipeline.DrawLine(new Line(this.Value.Nodes[element.I], this.Value.Nodes[element.J]), args.Color);
                    
                }

            }
     
        }
        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            //No meshes are drawn.   
        }
        #endregion
    }
}