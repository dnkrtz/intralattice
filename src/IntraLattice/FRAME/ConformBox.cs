using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino;

// This component generates a simple cartesian 3D lattice grid.
// ============================================================
// Includes comments explaining the purpose of each method of a C# Grasshopper Component (GH_Component).

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class ConformBox : GH_Component
    {

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ConformBox()
            : base("ConformBox", "ConformBox",
                "Generates a lattice grid box.",
                "IntraLattice2", "Frame")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.list);
            pManager.AddNumberParameter("Cell Size ( x )", "CSx", "Size of unit cell (x)", GH_ParamAccess.item, 5); // '5' is the default value
            pManager.AddNumberParameter("Cell Size ( y )", "CSy", "Size of unit cell (y)", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Cell Size ( z )", "CSz", "Size of unit cell (z)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number of Cells ( x )", "Nx", "Number of unit cells (x)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number of Cells ( y )", "Ny", "Number of unit cells (y)", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("Number of Cells ( z )", "Nz", "Number of unit cells (z)", GH_ParamAccess.item, 5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Grid", "G", "Point grid", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables and assign initial invalid data.
            //    This way, if the input parameters fail to supply valid data, we know when to abort
            var topology = new List<Curve>();
            double xCellSize = 0;
            double yCellSize = 0;
            double zCellSize = 0;
            int nX = 0;
            int nY = 0;
            int nZ = 0;

            // 2. Retrieve input data.
            if (!DA.GetDataList(0, topology)) { return; }
            if (!DA.GetData(1, ref xCellSize)) { return; }
            if (!DA.GetData(2, ref yCellSize)) { return; }
            if (!DA.GetData(3, ref zCellSize)) { return; }
            if (!DA.GetData(4, ref nX)) { return; }
            if (!DA.GetData(5, ref nY)) { return; }
            if (!DA.GetData(6, ref nZ)) { return; }

            // 3. If data is invalid, we need to abort.
            if (topology.Count < 2) { return; }
            if (xCellSize == 0) { return; }
            if (yCellSize == 0) { return; }
            if (zCellSize == 0) { return; }
            if (nX == 0) { return; }
            if (nY == 0) { return; }
            if (nZ == 0) { return; }

            // 4. Declare our point grid datatree
            var nodeTree = new GH_Structure<GH_Point>();

            // 5. Prepare normalized unit cell topology
            var cellNodes = new Point3dList();
            var cellStruts = new List<IndexPair>();
            TopologyTools.Topologize(ref topology, ref cellNodes, ref cellStruts);  // converts list of lines into an adjacency list format (cellNodes and cellStruts)
            TopologyTools.NormaliseTopology(ref cellNodes); // normalizes the unit cell (scaled to unit size and moved to origin)
            
            // 6. Define BasePlane and directional iteration vectors
            Plane basePlane = Plane.WorldXY;
            Vector3d vectorX = xCellSize * basePlane.XAxis;
            Vector3d vectorY = yCellSize * basePlane.YAxis;
            Vector3d vectorZ = zCellSize * basePlane.ZAxis;

            // 7. Map nodes to design space
            //    Loop through the uvw cell grid
            for (int u = 0; u <= nX; u++)
            {
                for (int v = 0; v <= nY; v++)
                {
                    for (int w = 0; w <= nZ; w++)
                    {
                        // this loop maps each node in the cell onto the UV-surface maps
                        for (int nodeIndex = 0; nodeIndex < cellNodes.Count; nodeIndex++)
                        {

                            double usub = cellNodes[nodeIndex].X; // u-position within unit cell
                            double vsub = cellNodes[nodeIndex].Y; // v-position within unit cell
                            double wsub = cellNodes[nodeIndex].Z; // w-position within unit cell

                            // compute position vector
                            Vector3d V = (u+usub) * vectorX + (v+vsub) * vectorY + (w+wsub) * vectorZ;
                            Point3d newPt = basePlane.Origin + V;

                            GH_Path treePath = new GH_Path(u, v, w);            // construct path in tree
                            nodeTree.Append(new GH_Point(newPt), treePath);     // add point to tree
                        }
                    }
                }
            }

            // 8. Set output
            DA.SetDataTree(0, nodeTree);
        }
        
        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
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
            get { return new Guid("{3d9572a6-0783-4885-9b11-df464cf549a7}"); }
        }
    }
}
