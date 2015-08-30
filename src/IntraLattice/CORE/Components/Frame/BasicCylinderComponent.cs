using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino;
using IntraLattice.Properties;
using Grasshopper;
using IntraLattice.CORE.Data;
using IntraLattice.CORE.Components;
using IntraLattice.CORE.Helpers;

// Summary:     This component generates a simple cylindrical lattice.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components
{
    public class BasicCylinderComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the BasicCylinderComponent class.
        /// </summary>
        public BasicCylinderComponent()
            : base("Basic Cylinder", "BasicCylinder",
                "Generates a conformal lattice cylinder.",
                "IntraLattice", "Frame")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.item);
            pManager.AddNumberParameter("Radius", "R", "Radius of cylinder", GH_ParamAccess.item, 15);
            pManager.AddNumberParameter("Height", "H", "Height of cylinder", GH_ParamAccess.item, 25);
            pManager.AddIntegerParameter("Number u", "Nu", "Number of unit cells (axial)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number v", "Nv", "Number of unit cells (theta)", GH_ParamAccess.item, 15);
            pManager.AddIntegerParameter("Number w", "Nw", "Number of unit cells (radial)", GH_ParamAccess.item, 4);
            pManager.AddBooleanParameter("Morph", "Morph", "If true, struts are morphed to the space as curves.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Strut curve network", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate data
            var cell = new UnitCell();
            double radius = 0;
            double height = 0;
            int nU = 0;
            int nV = 0;
            int nW = 0;
            bool morphed = false;

            if (!DA.GetData(0, ref cell)) { return; }
            if (!DA.GetData(1, ref radius)) { return; }
            if (!DA.GetData(2, ref height)) { return; }
            if (!DA.GetData(3, ref nU)) { return; }
            if (!DA.GetData(4, ref nV)) { return; }
            if (!DA.GetData(5, ref nW)) { return; }
            if (!DA.GetData(6, ref morphed)) { return; }

            if (!cell.isValid) { return; }
            if (radius == 0) { return; }
            if (height == 0) { return; }
            if (nU == 0) { return; }
            if (nV == 0) { return; }
            if (nW == 0) { return; }

            // 2. Initialize the lattice
            var lattice = new Lattice();
            // Will contain the morphed uv spaces (as surface-surface, surface-axis or surface-point)
            var spaceTree = new DataTree<GeometryBase>(); 
            
            // 3. Define cylinder
            Plane basePlane = Plane.WorldXY;
            Surface cylinder = ( new Cylinder(new Circle(basePlane, radius), height) ).ToNurbsSurface();
            cylinder = cylinder.Transpose();
            LineCurve axis = new LineCurve(basePlane.Origin, basePlane.Origin + height*basePlane.ZAxis);

            // 4. Package the number of cells in each direction into an array
            float[] N = new float[3] { nU, nV, nW };

            // 5. Normalize the UV-domain
            Interval unitDomain = new Interval(0, 1);
            cylinder.SetDomain(0, unitDomain); // surface u-direction
            cylinder.SetDomain(1, unitDomain); // surface v-direction
            axis.Domain = unitDomain;

            // 6. Prepare cell (this is a UnitCell object)
            cell = cell.Duplicate();
            cell.FormatTopology();

            // 7. Map nodes to design space
            //    Loop through the uvw cell grid
            for (int u = 0; u <= N[0]; u++)
            {
                for (int v = 0; v <= N[1]; v++)
                {
                    for (int w = 0; w <= N[2]; w++)
                    {
                        // Construct cell path in tree
                        GH_Path treePath = new GH_Path(u, v, w);
                        // Fetch the list of nodes to append to, or initialise it
                        var nodeList = lattice.Nodes.EnsurePath(treePath);      

                        // This loop maps each node index in the cell onto the UV-surface maps
                        for (int i = 0; i < cell.Nodes.Count; i++)
                        {
                            double usub = cell.Nodes[i].X; // u-position within unit cell (local)
                            double vsub = cell.Nodes[i].Y; // v-position within unit cell (local)
                            double wsub = cell.Nodes[i].Z; // w-position within unit cell (local)
                            double[] uvw = { u + usub, v + vsub, w + wsub }; // uvw-position (global)

                            // Check if the node belongs to another cell (i.e. it's relative path points outside the current cell)
                            bool isOutsideCell = (cell.NodePaths[i][0] > 0 || cell.NodePaths[i][1] > 0 || cell.NodePaths[i][2] > 0);
                            // Check if current uvw-position is beyond the upper boundary
                            bool isOutsideSpace = (uvw[0] > N[0] || uvw[1] > N[1] || uvw[2] > N[2]);

                            if (isOutsideCell || isOutsideSpace)
                            {
                                nodeList.Add(null);
                            }
                            else
                            {
                                Point3d pt1, pt2;
                                Vector3d[] derivatives;

                                // Construct z-position vector
                                Vector3d vectorZ = height * basePlane.ZAxis * uvw[0] / N[0];
                                // Compute pt1 (on axis)
                                pt1 = basePlane.Origin + vectorZ;
                                // Compute pt2 (on surface)
                                cylinder.Evaluate(uvw[0] / N[0], uvw[1] / N[1], 2, out pt2, out derivatives);       

                                // Create vector joining these two points
                                Vector3d wVect = pt2 - pt1;
                                // Instantiate new node
                                var newNode = new LatticeNode(pt1 + wVect * uvw[2] / N[2]);
                                // Add new node to tree
                                nodeList.Add(newNode); 
                            }
                        }
                    }

                    // Define the uv space map tree (used for morphing)
                    if (morphed && u < N[0] && v < N[1])
                    {
                        GH_Path spacePath = new GH_Path(u, v);
                        // Set trimming interval
                        var uInterval = new Interval((u) / N[0], (u + 1) / N[0]);                   
                        var vInterval = new Interval((v) / N[1], (v + 1) / N[1]);
                        // Create sub-surface and sub axis
                        Surface ss1 = cylinder.Trim(uInterval, vInterval);                          
                        Curve ss2 = axis.Trim(uInterval);
                        // Unitize domains
                        ss1.SetDomain(0, unitDomain); ss1.SetDomain(1, unitDomain);                 
                        ss2.Domain = unitDomain;
                        // Save to the space tree
                        spaceTree.Add(ss1, spacePath);
                        spaceTree.Add(ss2, spacePath);
                    }
                }
            }

            // 8. Map struts to the node tree
            if (morphed)
            {
                lattice.MorphMapping(cell, spaceTree, N);
            }
            else
            {
                lattice.ConformMapping(cell, N);
            }

            // 9. Set output
            DA.SetDataList(0, lattice.Struts);            
        }

        /// <summary>
        /// Sets the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resources.basicCylinder;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{9f6769c0-dec5-4a0d-8ade-76fca1dfd4e3}"); }
        }
    }
}
