using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Intersect;

// This component generates a conformal lattice grid based on a cylindrical coordinate system.
// It takes as input a curve (axis) and a surface.
// Assumption : The surface rotates the full 360degrees around the axis (for now).

namespace ConformSA
{
    public class ConformSAComponent : GH_Component
    {
        public ConformSAComponent()
            : base("ConformSA", "ConfSA",
                "Generates lattice grid conformed to surface and axis",
                "IntraLattice2", "Grid")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Surface to conform to", GH_ParamAccess.item);
            pManager.AddCurveParameter("Axis", "A", "Axis (may be curved)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number r", "rNum", "# of unit cells in radial direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number t", "tNum", "# of unit cells in tangential direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number z", "zNum", "# of unit cells in z-direction", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Point Grid", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables and assign initial invalid data.
            Surface S = null;
            Curve A = null;
            double rNum = 0;
            double tNum = 0;
            double zNum = 0;

            // Attempt to fetch data
            if (!DA.GetData(0, ref S)) { return; }
            if (!DA.GetData(1, ref A)) { return; }
            if (!DA.GetData(2, ref rNum)) { return; }
            if (!DA.GetData(3, ref tNum)) { return; }
            if (!DA.GetData(4, ref zNum)) { return; }

            // Validate data
            if (!S.IsValid) { return; }
            if (!A.IsValid) { return; }
            if (rNum == 0) { return; }
            if (tNum == 0) { return; }
            if (zNum == 0) { return; }

            // Initialize the grid of points
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();

            // Prepare divisions along axis ('zNum' divisions)
            List<double> Params = new List<double>(A.DivideByCount((int)zNum, true)); // cast array to list
            Plane[] BasePlanes = A.GetPerpendicularFrames(Params);

            // Prepare angles about axis ('tNum' divisions)
            // For now, assuming surface covers full 360degree rotation
            List<double> Angles = new List<double>();
            for (int i = 0; i<tNum; i++) Angles.Add(2*Math.PI*i/tNum);

            // Loop along axis
            for (int i=0; i<BasePlanes.Length; i++)
            {
                Plane BasePlane = BasePlanes[i];
                // Loop about axis
                for (int j=0; j<Angles.Count; j++)
                {
                    double Angle = Angles[j];
                    Vector3d RVect = BasePlane.PointAt(Math.Cos(Angle), Math.Sin(Angle)) - BasePlane.Origin; // Radial unit vector
                    Ray3d RRay = new Ray3d(BasePlane.Origin, RVect);
                    Point3d SurfPt = Intersection.RayShoot(RRay,new List<Surface>{S},1)[0];   // Shoot ray to intersect surface
                    RVect = SurfPt - BasePlane.Origin;  // Update radial vector (changes amplitude, direction unchanged)

                    // Loop away from axis
                    for (int k=0; k<=rNum; k++)
                    {
                        Point3d NewPt = BasePlane.Origin + RVect * k / rNum;

                        GH_Path TreePath = new GH_Path(0, i, j);           // Construct path in the tree
                        GridTree.Append(new GH_Point(NewPt), TreePath);    // Add point to GridTree
                    }
                }   
            }

            // Output grid
            DA.SetDataTree(0, GridTree);

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

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{e0e8a858-66bd-4145-b173-23dc2e247206}"); }
        }
    }
}
