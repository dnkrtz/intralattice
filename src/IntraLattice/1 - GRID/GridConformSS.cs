using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;

// This component generates a conformal lattice grid between two surfaces.
// =======================================================================
// Assumption : The surfaces are oriented in the same direction (for UV-Map indices)

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class GridConformSS : GH_Component
    {
        public GridConformSS()
            : base("Conform Surface-Surface", "ConformSS",
                "Generates a conforming point grid between two surfaces.",
                "IntraLattice2", "Grid")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface 1", "S1", "First bounding surface", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface 2", "S2", "Second bounding surface", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip UV", "FlipUV", "Flip the UV parameters (for alignment purposes)", GH_ParamAccess.item, false); // default value is false
            pManager.AddNumberParameter("Number u", "Nu", "Number of unit cells (u)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number v", "Nv", "Number of unit cells (v)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Number w", "Nw", "Number of unit cells (w)", GH_ParamAccess.item, 5);
            pManager.AddBooleanParameter("Morph", "Morph", "If true, struts will morph to the design space (as bezier curves)", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Morph Factor", "MF", "Division factor for bezier vectors (recommended: 2.0-3.0)", GH_ParamAccess.item, 3);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "Grid", "Point grid", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Strut lines", "Lines", "Strut line network", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables
            int topology = 0;
            Surface s1 = null;
            Surface s2 = null;
            bool flipUV = false;
            double nU = 0;
            double nV = 0;
            double nW = 0;
            bool morphed = false;
            double morphFactor = 0;

            // 2. Attempt to fetch data
            if (!DA.GetData(0, ref topology)) { return; }
            if (!DA.GetData(1, ref s1)) { return; }
            if (!DA.GetData(2, ref s2)) { return; }
            if (!DA.GetData(3, ref flipUV)) { return; }
            if (!DA.GetData(4, ref nU)) { return; }
            if (!DA.GetData(5, ref nV)) { return; }
            if (!DA.GetData(6, ref nW)) { return; }
            if (!DA.GetData(7, ref morphed)) { return; }
            if (!DA.GetData(8, ref morphFactor)) { return; }

            // 3. Validate data
            if (!s1.IsValid) { return; }
            if (!s2.IsValid) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // 4. Initialize the grid tree and derivatives tree
            var gridTree = new GH_Structure<GH_Point>();     // will contain point grid
            var derivTree = new GH_Structure<GH_Vector>();   // will contain derivatives (du,dv) in a parallel tree

            // 5. Flip the UV parameters a surface if specified
            if (flipUV) s1 = s1.Transpose();
            
            // 6. Package the number of increments in each direction in an array
            double[] N = new double[3] { nU, nV, nW };

            // 7. Normalize the UV-domain
            Interval normalizedDomain = new Interval(0,1);
            s1.SetDomain(0, normalizedDomain); // s1 u-direction
            s1.SetDomain(1, normalizedDomain); // s1 v-direction
            s2.SetDomain(0, normalizedDomain); // s2 u-direction
            s2.SetDomain(1, normalizedDomain); // s2 v-direction

            // 8. Let's create the grid of cell corners now
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    Point3d pt1; // On surface 1
                    Vector3d[] derivatives1;
                    Point3d pt2; // On surface 2
                    Vector3d[] derivatives2;
                    
                    // evaluate point and its derivatives on both surface
                    s1.Evaluate(u/nU, v/nV, 2, out pt1, out derivatives1);
                    s2.Evaluate(u/nU, v/nV, 2, out pt2, out derivatives2);


                    // create vector joining the two points
                    Vector3d wVect = pt2 - pt1;

                    // create grid points on and between surfaces
                    for (int w = 0; w <= N[2]; w++)
                    {
                        GH_Path treePath = new GH_Path(u, v, w);    // path in the trees

                        // save point to gridTree
                        Point3d newPt = pt1 + wVect * w / N[2];
                        gridTree.Append(new GH_Point(newPt), treePath);
                        
                        // for each of the 2 directional directives
                        for (int derivIndex = 0; derivIndex < 2; derivIndex++ )
                        {
                            // compute the interpolated derivative (need interpolation for in-between surfaces)
                            double interpolationFactor = w/N[2];
                            Vector3d deriv = derivatives1[derivIndex] + interpolationFactor*(derivatives2[derivIndex]-derivatives1[derivIndex]);
                            derivTree.Append(new GH_Vector(deriv), treePath);
                        }
                    }
                }
            }

            // Let's map the topology to this grid
            // 9. Now we create lattice struts
            var struts = new List<GH_Curve>();
            //    Loop over all nodes in the grid
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {

                        // We'll be needing the data tree path of the current node, and those of its neighbours
                        GH_Path currentPath = new GH_Path(u, v, w);
                        List<GH_Path> neighbourPaths = new List<GH_Path>();

                        // Get neighbours!!
                        FrameTools.TopologyNeighbours(ref neighbourPaths, topology, N, u, v, w);

                        // Nere we create the actual struts
                        // Firt, make sure currentpath exists in the tree
                        if (gridTree.PathExists(currentPath))
                        {
                            // Connect current node to all its neighbours
                            Point3d pt1 = gridTree[currentPath][0].Value;   // current node

                            // Cycle through all neighbours
                            foreach (GH_Path neighbourPath in neighbourPaths)
                            {
                                // Make sure the neighbourpath exists in the tree
                                if (gridTree.PathExists(neighbourPath))
                                {
                                    Point3d pt2 = gridTree[neighbourPath][0].Value; // neighbour node

                                    // Create the strut
                                    // If 'curved' is true, we create a bezier curve based on the uv derivatives
                                    if (morphed)
                                    {
                                        List<Vector3d> du = new List<Vector3d>();
                                        List<Vector3d> dv = new List<Vector3d>();
                                        List<Vector3d> duv = new List<Vector3d>();

                                        // If neighbours in u-direction
                                        if (neighbourPath.Indices[0] != currentPath.Indices[0])
                                        {
                                            // Notice morphFactor here, it scales the vector to give a better morphing
                                            du.Add(derivTree[currentPath][0].Value / (morphFactor * N[0]));   // du[0] is the current node derivative
                                            du.Add(derivTree[neighbourPath][0].Value / (morphFactor * N[0])); // du[1] is the neighbour node derivative
                                        }
                                        // If neighbours in v-direction
                                        if (neighbourPath.Indices[1] != currentPath.Indices[1])
                                        {
                                            dv.Add(derivTree[currentPath][1].Value / (morphFactor * N[1]));   // dv[0] is the current node derivative
                                            dv.Add(derivTree[neighbourPath][1].Value / (morphFactor * N[1])); // dv[1] is the neighbour node derivative
                                        }
                                        // If neighbours in uv-direction (diagonal)
                                        if (du.Count == 2 && dv.Count == 2)
                                        {
                                            // Compute cross-derivative
                                            duv.Add((du[0] + dv[0]) / (Math.Sqrt(2)));  // duv[0] is the current node derivative
                                            duv.Add((du[1] + dv[1]) / (Math.Sqrt(2)));  // duv[1] is the neighbour node derivative
                                        }
                                        
                                        // Now we have everything we need to build a bezier curve
                                        List<Point3d> controlPoints = new List<Point3d>();
                                        controlPoints.Add(pt1); // first control point (vertex)
                                        if (duv.Count == 2)
                                        {
                                            if (neighbourPath.Indices[0] > currentPath.Indices[0] && neighbourPath.Indices[1] > currentPath.Indices[1])
                                            {
                                                controlPoints.Add(pt1 + duv[0]);
                                                controlPoints.Add(pt2 - duv[1]);
                                            }
                                            else if (neighbourPath.Indices[0] < currentPath.Indices[0] && neighbourPath.Indices[1] < currentPath.Indices[1])
                                            {
                                                controlPoints.Add(pt1 - duv[0]);
                                                controlPoints.Add(pt2 + duv[1]);
                                            }
                                            else if (neighbourPath.Indices[0] > currentPath.Indices[0] && neighbourPath.Indices[1] < currentPath.Indices[1])
                                            {
                                                controlPoints.Add(pt1 + ((du[0] - dv[0]) / (Math.Sqrt(2))));
                                                controlPoints.Add(pt2 - ((du[1] - dv[1]) / (Math.Sqrt(2))));
                                            }
                                            else
                                            {
                                                controlPoints.Add(pt1 - ((du[0] - dv[0]) / (Math.Sqrt(2))));
                                                controlPoints.Add(pt2 + ((du[1] - dv[1]) / (Math.Sqrt(2))));
                                            }
                                        }
                                        else if (du.Count == 2)
                                        {
                                            controlPoints.Add(pt1 + du[0]);
                                            controlPoints.Add(pt2 - du[1]);
                                        }
                                        else if (dv.Count == 2)
                                        {
                                            controlPoints.Add(pt1 + dv[0]);
                                            controlPoints.Add(pt2 - dv[1]);
                                        }
                                        controlPoints.Add(pt2); // fourth control point (vertex)
                                        BezierCurve curve = new BezierCurve(controlPoints);

                                        struts.Add(new GH_Curve(curve.ToNurbsCurve()));
                                    }
                                    // If not 'curved', or in w-direction, create a simple linear strut
                                    else
                                        struts.Add(new GH_Curve(new LineCurve(new Line(pt1, pt2))));
                                }
                            }
                        }

                    }
                }
            }



            // 9. Set output
            DA.SetDataTree(0, gridTree);
            DA.SetDataList(1, struts);

        }

        // Conform components are in second slot of the grid category
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

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{ac0814b4-00e7-4efb-add5-e845a831c6da}"); }
        }
    }
}
