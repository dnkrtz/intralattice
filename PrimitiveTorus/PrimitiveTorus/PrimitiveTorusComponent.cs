using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace PrimitiveTorus
{
    public class PrimitiveTorusComponent : GH_Component
    {
        public PrimitiveTorusComponent()
            : base("PrimitiveTorus", "PTorus",
                "Generates a simple torus lattice",
                "IntraLattice2", "Wireframe")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Donut Radius", "Rd", "Donut radius (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Outer Radius", "Ro", "Cylinder outer radius (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Inner Radius", "Ri", "Cylinder inner radius (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number r", "rNum", "# of unit cells in radial direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number t", "tNum", "# of unit cells in tangential direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number d", "dNum", "# of unit cells in donut-direction", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Point Grid", GH_ParamAccess.tree);
            pManager.AddLineParameter("Lines", "L", "Lattice Wireframe", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables and assign initial invalid data.
            //    This way, if the input parameters fail to supply valid data, we know when to abort.
            double d_radius = 0;
            double o_radius = 0;
            double i_radius = 0;
            double rNum = 0;
            double tNum = 0;
            double dNum = 0;

            // 2. Retrieve input data.
            if (!DA.GetData(0, ref d_radius)) { return; }
            if (!DA.GetData(1, ref o_radius)) { return; }
            if (!DA.GetData(2, ref i_radius)) { return; }
            if (!DA.GetData(3, ref rNum)) { return; }
            if (!DA.GetData(4, ref tNum)) { return; }
            if (!DA.GetData(5, ref dNum)) { return; }

            // 3. If data is invalid, we need to abort.
            if (d_radius == 0) { return; }
            if (o_radius == 0) { return; }
            if (rNum == 0) { return; }
            if (tNum == 0) { return; }
            if (dNum == 0) { return; }

            //// 4. Let's do some real stuffz
            // Declare gh_structure data tree
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();

            Plane plane = Plane.WorldXY;
            Point3d basePoint = plane.Origin;

            double rSize = (o_radius - i_radius) / rNum;
            double tSize = 2 * Math.PI / tNum;
            double dSize = 2 * Math.PI / dNum;

            // Create grid of points (in data tree structure)

            for (int i = 0; i <= dNum; i++)
            {

                for (int j = 0; j <= tNum; j++)
                {

                    for (int k = 0; k <= rNum; k++)
                    {

                        Vector3d V = new Vector3d((d_radius + k * rSize * Math.Cos(j * tSize)) * Math.Cos(i * dSize), (d_radius + k * rSize * Math.Cos(j * tSize)) * Math.Sin(i * dSize), k * rSize * Math.Sin(j * tSize));

                        Point3d newPt = basePoint + V;

                        // Construct path in the 3-branch tree (custom data structure)
                        GH_Path pth = new GH_Path(0, i, j);

                        GridTree.Append(new GH_Point(newPt), pth);
                    }
                }
            }

            List<GH_Line> Struts = new List<GH_Line>();

            for (int i = 0; i <= dNum; i++)
            {
                for (int j = 0; j <= tNum; j++)
                {
                    for (int k = 0; k <= rNum; k++)
                    {
                        if (j < tNum)
                        {
                            GH_Path pth1 = new GH_Path(0, i, j);
                            GH_Path pth2 = new GH_Path(0, i, j + 1);
                            Point3d node1 = GridTree[pth1][k].Value;
                            Point3d node2 = GridTree[pth2][k].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                        if (k < rNum)
                        {
                            GH_Path pth1 = new GH_Path(0, i, j);
                            GH_Path pth2 = new GH_Path(0, i, j);
                            Point3d node1 = GridTree[pth1][k].Value;
                            Point3d node2 = GridTree[pth2][k + 1].Value;

                            Struts.Add(new GH_Line(new Line(node1, node2)));
                        }
                        if (i < dNum)
                        {
                            GH_Path pth1 = new GH_Path(0, i, j);
                            GH_Path pth2 = new GH_Path(0, i + 1, j);
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
            get { return new Guid("{38d1abc3-c997-4751-a673-4ce9e99227b7}"); }
        }
    }
}
