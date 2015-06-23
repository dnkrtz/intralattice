using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;

// This component maps a unit cell topology to the lattice grid
// ============================================================
// Also TRIMS the resulting lattice to the shape of the design space
// 

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class FrameUniform : GH_Component
    {
        public FrameUniform()
            : base("FrameUniform", "FrameUnif",
                "Populates grid with lattice topology (and trims to design space)",
                "IntraLattice2", "Frame")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point Grid", "G", "Conformal lattice grid", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Design Space", "DS", "Design space to trim with (Brep or Mesh)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Topology", "Topo", "Unit cell topology\n0 - grid\n1 - x\n2 - star\n3 - star2\n4 - octa)", GH_ParamAccess.item, 0);
            
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Lattice frame", "L", "Lattice list", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables and assign initial invalid data.
            int topo = 0;
            GeometryBase designSpace = null;
            GH_Structure<GH_Point> gridTree = null;

            // Attempt to fetch data
            if (!DA.GetDataTree(0, out gridTree)) { return; }
            if (!DA.GetData(1, ref designSpace)) { return; }
            if (!DA.GetData(2, ref topo)) { return; }

            // Validate data
            if (gridTree == null) { return; }
            if (!designSpace.IsValid) { return; }
            if (designSpace.ObjectType != ObjectType.Brep && designSpace.ObjectType != ObjectType.Mesh) { return; }

            // Get domain size of the tree
            int[] N = new int[] {0,0,0};
            foreach (GH_Path path in gridTree.Paths)
            {
                if ( path.Indices[0] > N[0] ) N[0] = path.Indices[0];
                if ( path.Indices[1] > N[1] ) N[1] = path.Indices[1];
                if ( path.Indices[2] > N[2] ) N[2] = path.Indices[2];
            }

            // Initiate list of lattice lines
            List<GH_Line> struts = new List<GH_Line>();

            for (int i = 0; i <= N[0]; i++)
            {
                for (int j = 0; j <= N[1]; j++)
                {
                    for (int k = 0; k <= N[2]; k++)
                    {
                        
                        // We'll be needing the data tree path of the current node, and those of its neighbours
                        GH_Path currentPath = new GH_Path(i,j,k);
                        List<GH_Path> neighbourPaths = new List<GH_Path>();
                     
                        // Get neighbours!!
                        FrameTools.TopologyNeighbours(ref neighbourPaths, topo, N, i, j, k);

                        // Nere we create the actual struts
                        // First, make sure currentpath exists in the tree
                        if (gridTree.PathExists(currentPath))
                        {
                            // Connect current node to all its neighbours
                            Point3d node1 = gridTree[currentPath][0].Value;
                            foreach (GH_Path neighbourPath in neighbourPaths)
                            {
                                // Again, make sure the neighbourpath exists in the tree
                                if (gridTree.PathExists(neighbourPath))
                                {
                                    Point3d node2 = gridTree[neighbourPath][0].Value;
                                    LineCurve strut = new LineCurve(new Line(node1, node2), 0, 1);  // set line, with curve parameter domain [0,1]

                                    // For BREP design space
                                    if (designSpace.ObjectType == ObjectType.Brep)
                                    {
                                        Point3d[] intersectionPoint = null; // the intersection point
                                        Curve[] overlapCurves = null;   // dummy variable for CurveBrep call
                                        bool[] nodeInside = new bool[2] { false, false };

                                        // Could do this in the grid section (set bool values)
                                        if (((Brep)designSpace).IsPointInside(node1, Rhino.RhinoMath.SqrtEpsilon, false))
                                            nodeInside[0] = true;
                                        if (((Brep)designSpace).IsPointInside(node2, Rhino.RhinoMath.SqrtEpsilon, false))
                                            nodeInside[1] = true;

                                        // Now perform checks
                                        if (!nodeInside[0] && !nodeInside[1])
                                            continue; // if neither node is inside, don't create a strut, skip to next loop
                                        else if (nodeInside[0] && nodeInside[1])
                                            struts.Add(new GH_Line(strut.Line)); // if both nodes are inside, add full strut
                                        else
                                        {
                                            // if nodes are on opposite sides of the space boundary, TRIM
                                            // Begin by intersecting the brep
                                            Intersection.CurveBrep(strut, ((Brep)designSpace), Rhino.RhinoMath.SqrtEpsilon, out overlapCurves, out intersectionPoint);

                                            // If no intersection was found, something went wrong, don't create a strut, skip to next loop
                                            if (intersectionPoint == null) continue;
                                            else
                                            {
                                                if (nodeInside[0]) struts.Add(new GH_Line(new Line(node1, intersectionPoint[0])));
                                                else struts.Add(new GH_Line(new Line(node2, intersectionPoint[0])));
                                            }
                                        }
                                    }
                                    // For MESH design space
                                    else if (designSpace.ObjectType == ObjectType.Mesh) ;
                                        //Intersection.MeshLine((Mesh)designSpace, strut.Line, );

                                }
                            }
                        }
                    }
                }
            }
      

            // Output grid
            DA.SetDataList(0, struts);
        }

        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{c60d6bd4-083b-4b54-b840-978d251d9653}"); }
        }
    }
}
