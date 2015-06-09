using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace PrimitiveCylinder
{
    public class PrimitiveCylinderComponent : GH_Component
    {
       
        /// <summary>
        /// Public constructor
        /// </summary>
        public PrimitiveCylinderComponent()
            : base("PrimitiveCylinder", "PCylinder",
                "Description",
                "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Outer Radius", "Ro", "Cylinder outer radius (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Inner Radius", "Ri", "Cylinder inner radius (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Cylinder height (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number r", "rNum", "# of unit cells in radial direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number t", "tNum", "# of unit cells in tangential direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number z", "zNum", "# of unit cells in z-direction", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Point Grid", GH_ParamAccess.tree);
            pManager.AddLineParameter("Lines", "L", "Lattice Wireframe", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables and assign initial invalid data.
            //    This way, if the input parameters fail to supply valid data, we know when to abort.
            double o_radius = 0;
            double i_radius = 0;
            double height = 0;
            double rNum = 0;
            double tNum = 0;
            double zNum = 0;

            // 2. Retrieve input data.
            if (!DA.GetData(0, ref o_radius)) { return; }
            if (!DA.GetData(1, ref i_radius)) { return; }
            if (!DA.GetData(2, ref height)) { return; }
            if (!DA.GetData(3, ref rNum)) { return; }
            if (!DA.GetData(4, ref tNum)) { return; }
            if (!DA.GetData(5, ref zNum)) { return; }

            // 3. If data is invalid, we need to abort.
            if (o_radius == 0) { return; }
            if (height == 0) { return; }
            if (rNum == 0) { return; }
            if (tNum == 0) { return; }
            if (zNum == 0) { return; }

            //// 4. Let's do some real stuffz
            // Declare gh_structure data tree
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();

            Plane plane = Plane.WorldXY;
            Point3d basePoint = plane.Origin;

            Vector3d Zdir = plane.ZAxis * (height / zNum);

            double rSize = (o_radius - i_radius) / rNum;
            double tSize = 2 * Math.PI / tNum;

            // Create grid of points (in data tree structure)
            // Tangential loop
            for (int i = 0; i <= tNum; i++)
            {
                // tMult is the tangentially oriented unit vector
                Vector3d tMult = (new Vector3d(Math.Cos(i * tSize), Math.Sin(i * tSize), 0));
                
                // Height loop
                for (int j = 0; j <= zNum; j++)
                {
                    Vector3d Vz = Zdir * j;

                    // Radial loop
                    for (int k = 0; k <= rNum; k++)
                    {
                        Vector3d Vr = k * rSize * tMult;

                        Point3d newPt = basePoint + tMult * (i_radius) + Vr + Vz;

                        GH_Path pth = new GH_Path(0, j, i);           // Construct path in the tree
                        GridTree.Append(new GH_Point(newPt), pth);    // Add point to GridTree
                    }
                }
            }


            List<GH_Line> Struts = new List<GH_Line>();

            for (int i = 0; i <= tNum; i++)
            {
                for (int j = 0; j <= zNum; j++)
                {
                    for (int k = 0; k <= rNum; k++)
                    {
                        if (i < tNum)
                        {
                            GH_Path pth1 = new GH_Path(0, j, i);
                            GH_Path pth2 = new GH_Path(0, j, i + 1);
                            Point3d node1 = GridTree[pth1][k].Value;
                            Point3d node2 = GridTree[pth2][k].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                        if (k < rNum)
                        {
                            GH_Path pth1 = new GH_Path(0, j, i);
                            GH_Path pth2 = new GH_Path(0, j, i);
                            Point3d node1 = GridTree[pth1][k].Value;
                            Point3d node2 = GridTree[pth2][k + 1].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                        if (j < zNum)
                        {
                            GH_Path pth1 = new GH_Path(0, j, i);
                            GH_Path pth2 = new GH_Path(0, j + 1, i);
                            Point3d node1 = GridTree[pth1][k].Value;
                            Point3d node2 = GridTree[pth2][k].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                    }
                }
            }

            // Set output
            DA.SetDataTree(0, GridTree);
            DA.SetDataList(1, Struts);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
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
            get { return new Guid("{2f8db308-ace2-43c1-a962-110abe609e20}"); }
        }
    }
}
