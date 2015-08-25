using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace IntraLattice.CORE.Data.GH_Goo
{
    public class UnitCellGoo : Grasshopper.Kernel.Types.GH_GeometricGoo<UnitCell>, IGH_PreviewData
    {
        #region Constructors
        public UnitCellGoo()
        {
            this.Value = new UnitCell();
        }
        public UnitCellGoo(UnitCell cell)
        {
            if (cell == null)
            {
                cell = new UnitCell();
            }
            this.Value = cell;
        }
        public override Grasshopper.Kernel.Types.IGH_GeometricGoo DuplicateGeometry()
        {
            return DuplicateGoo();
        }
        public UnitCellGoo DuplicateGoo()
        {
            return new UnitCellGoo(Value == null ? new UnitCell() : Value.Duplicate());
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
        public override object ScriptVariable()
        {
            return this.Value;
        }
        public override bool CastTo<Q>(out Q target)
        {
            //Cast to LatticeCell.
            if (typeof(Q).IsAssignableFrom(typeof(UnitCell)))
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
            if (typeof(UnitCell).IsAssignableFrom(source.GetType()))
            {
                Value = (UnitCell)source;
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
            if (Value.Nodes != null)
            {
                foreach (var element in Value.Nodes) 
                {
                    args.Pipeline.DrawPoint(element, args.Color);
                }
            }
            if (Value.NodePairs != null)
            {
                foreach (var element in Value.NodePairs) 
                {
                    Point3d node1 = Value.Nodes[element.I];
                    Point3d node2 = Value.Nodes[element.J];
                    args.Pipeline.DrawLine(node1, node2, args.Color);
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