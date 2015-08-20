using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace IntraLattice.CORE.Data.GH_Goo
{
    public class LatticeGoo : Grasshopper.Kernel.Types.GH_GeometricGoo<Lattice>, IGH_PreviewData
    {
        #region Constructors
        public LatticeGoo(LatticeType type)
        {
            this.Value = new Lattice(type);
        }
        public LatticeGoo(Lattice cell)
        {
            if (cell == null)
            {
                cell = new Lattice(IntraLattice.CORE.Data.LatticeType.None);
            }
            this.Value = cell;
        }
        public LatticeGoo DuplicateGoo()
        {
            
            return new LatticeGoo(Value.Duplicate());
        }

        public override Grasshopper.Kernel.Types.IGH_GeometricGoo DuplicateGeometry()
        {
            return DuplicateGoo();
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
                if (Value.Nodes == null) { return "nodes empty"; }
                if (Value.Struts == null) { return "struts empty"; }
                return base.IsValidWhyNot;
            }
        }

        public override string ToString()
        {
            if (Value == null)
                return "Null Lattice";
            else
                return Value.ToString();
        }

        public override string TypeDescription
        {
            get { return ("Lattice Representation"); }
        }

        public override string TypeName
        {
            get { return "LatticeGoo"; }
        }

        public override Rhino.Geometry.BoundingBox Boundingbox
        {
            get {

                Rhino.Collections.Point3dList listofpoint = new Rhino.Collections.Point3dList(); 

                foreach (var element in Value.Nodes.AllData())
                {
                    listofpoint.Add(element.Point3d);
                }
                return listofpoint.BoundingBox;

                }
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
            if (typeof(Q).IsAssignableFrom(typeof(Lattice)))
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
            if (typeof(Lattice).IsAssignableFrom(source.GetType()))
            {
                Value = (Lattice)source;
                return true;
            }

            return false;
        }
        #endregion

        #region Transformation Methods
        public override Rhino.Geometry.BoundingBox GetBoundingBox(Rhino.Geometry.Transform xform)
        {
            throw new NotImplementedException();
        }

        public override Grasshopper.Kernel.Types.IGH_GeometricGoo Transform(Rhino.Geometry.Transform xform)
        {
            throw new NotImplementedException();
        }

        public override Grasshopper.Kernel.Types.IGH_GeometricGoo Morph(Rhino.Geometry.SpaceMorph xmorph)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Drawing Methods
        public Rhino.Geometry.BoundingBox ClippingBox
        {
            get { return Boundingbox; }
        }
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Value == null) { return; }
            if (Value.Struts != null)
            {
                foreach (LatticeStrut element in Value.Struts)
                {
                    args.Pipeline.DrawCurve(element.Curve, args.Color);
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
