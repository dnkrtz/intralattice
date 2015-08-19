using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;

namespace IntraLattice.CORE.Data.GH_Goo
{
    class LatticeGoo : Grasshopper.Kernel.Types.GH_GeometricGoo<Lattice>, IGH_PreviewData
    {
        public LatticeGoo(LatticeType type)
        {
            this.Value = new Lattice(type);
        }

        public override Grasshopper.Kernel.Types.IGH_Goo Duplicate()
        {
            return base.Duplicate();
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
                if (Value.Nodes == null) { return "nodes empty"; }
                if (Value.Struts == null) { return "struts empty"; }
                if(Value.Type == null){return "no type specified";}
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
                throw new NotImplementedException(); 
                }
        }

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

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            foreach (var element in Value.Struts) 
            {
                args.Pipeline.DrawCurve(element.Curve, args.Color);               
            }

        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            //No meshes are drawn.   
        }

    }
}
