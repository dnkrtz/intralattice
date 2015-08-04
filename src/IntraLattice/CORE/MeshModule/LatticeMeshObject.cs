using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino;

namespace IntraLattice.CORE.MeshModule
{
    public class LatticeMesh
    {
        // constructor
        public LatticeMesh()
        {
            this.Nodes = new List<Node>();
            this.Struts = new List<Strut>();
            this.Plates = new List<Plate>();
        }
        
        // fields
        public List<Node> Nodes { get; set; }
        public List<Strut> Struts { get; set; }
        public List<Plate> Plates { get; set; }
    }

    public class Node
    {
        // constructor
        public Node(Point3d SetPoint3d)
        {
            this.Point3d = SetPoint3d;
            this.StrutIndices = new List<int>();
            this.PlateIndices = new List<int>();
        }

        // fields
        public Point3d Point3d { get; set; }     // coordinates of node
        public double Radius { get; set; }       // strut radius at node
        public List<int> StrutIndices { get; set; } // relational data
        public List<int> PlateIndices { get; set; } // relational data

        
    }

    public class Strut
    {
        // constructor
        public Strut(Curve SetCurve, IndexPair SetNodePair)
        {
            this.Curve = SetCurve;
            this.NodePair = SetNodePair;
        }

        // fields
        public Curve Curve { get; set; }            // the strut curve
        public IndexPair NodePair { get; set; }
        public IndexPair PlatePair { get; set; }

       
    }

    public class Plate
    {
        // constructor
        public Plate(int SetNodeIndex, Vector3d SetNormal)
        {
            this.NodeIndex = SetNodeIndex;
            this.Normal = SetNormal;
            this.Vtc = new List<Point3d>();
        }

        //fields
        public double Offset { get; set; }      // offset value
        public Vector3d Normal { get; set; }    // plate normal
        public List<Point3d> Vtc { get; set; }  // vertices (at index 0 is the centerpoint vertex)
        public int NodeIndex { get; set; }      // relational data

        
    }
}
