using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace PrimitiveSphere
{
    public class PrimitiveSphereComponent : GH_Component
    {
        public PrimitiveSphereComponent()
            : base("PrimitiveSphere", "PSphere",
                "Generates a simple sphere lattice",
                "IntraLattice2", "Wireframe")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Radius", "Ro", "Sphere outer radius (mm)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Radius", "Ri", "Sphere inner radius (0 if full sphere)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number r", "rNum", "# of unit cells in radial direction", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number t", "tNum", "# of unit cells in tangential direction (horizontal rotation)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Number o", "oNum", "# of unit cells in theta-direction (vertical rotation)", GH_ParamAccess.item);
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
            double o_radius = 0;
            double i_radius = 0;
            double rNum = 0;
            double tNum = 0;
            double oNum = 0; // o for theta

            // 2. Retrieve input data.
            if (!DA.GetData(0, ref o_radius)) { return; }
            if (!DA.GetData(1, ref i_radius)) { return; }
            if (!DA.GetData(2, ref rNum)) { return; }
            if (!DA.GetData(3, ref tNum)) { return; }
            if (!DA.GetData(4, ref oNum)) { return; }

            // 3. If data is invalid, we need to abort.
            if (o_radius == 0) { return; }
            if (rNum == 0) { return; }
            if (tNum == 0) { return; }
            if (oNum == 0) { return; }

            //// 4. Let's do some real stuffz
            // Declare gh_structure data tree
            GH_Structure<GH_Point> GridTree = new GH_Structure<GH_Point>();

            Plane plane = Plane.WorldXY;
            Point3d basePoint = plane.Origin;

            double rSize = (o_radius - i_radius) / rNum;
            double tSize = 2 * Math.PI / tNum;
            double oSize = Math.PI / oNum;

            // Create grid of points (in data tree structure)

            // Tangential loop
            for (int i = 0; i <= tNum; i++)
            {
                double sint = Math.Sin(i * tSize);
                double cost = Math.Cos(i * tSize);
                // Theta loop
                for (int j = 0; j <= oNum; j++)
                {
                    double sino = Math.Sin(j * oSize);
                    double coso = Math.Cos(j * oSize);
                    Vector3d V = (new Vector3d(sino * cost, sino * sint, coso));
                    // Radial loop
                    for (int k = 0; k <= rNum; k++)
                    {
                        Vector3d Vr = k * rSize * V;

                        Point3d newPt = basePoint + V * (i_radius) + Vr;

                        // Construct path in the 3-branch tree (custom data structure)
                        GH_Path pth = new GH_Path(0, j, i);

                        GridTree.Append(new GH_Point(newPt), pth);
                    }
                }
            }


            List<GH_Line> Struts = new List<GH_Line>();

            for (int i = 0; i <= tNum; i++)
            {
                for (int j = 0; j <= oNum; j++)
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
                        if (j < oNum)
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
            get { return new Guid("{f70c9e3c-80cd-4dc5-a5b0-7113d44e9e10}"); }
        }
    }
}
