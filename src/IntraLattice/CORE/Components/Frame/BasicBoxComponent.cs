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
using IntraLattice.CORE.Data.GH_Goo;



// Summary:     This component generates a simple cartesian 3D lattice.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components
{
    public class BasicBoxComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the BasicBoxComponent class.
        /// </summary>
        public BasicBoxComponent()
            : base("Basic Box", "BasicBox",
                "Generates a lattice box.",
                "IntraLattice2", "Frame")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.item);
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
            pManager.AddCurveParameter("Struts", "Struts", "Strut curve network", GH_ParamAccess.list);
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
            var cell = new UnitCell();
            double xCellSize = 0;
            double yCellSize = 0;
            double zCellSize = 0;
            int nX = 0;
            int nY = 0;
            int nZ = 0;

            // 2. Retrieve input data.
            if (!DA.GetData(0, ref cell)) { return; }
            if (!DA.GetData(1, ref xCellSize)) { return; }
            if (!DA.GetData(2, ref yCellSize)) { return; }
            if (!DA.GetData(3, ref zCellSize)) { return; }
            if (!DA.GetData(4, ref nX)) { return; }
            if (!DA.GetData(5, ref nY)) { return; }
            if (!DA.GetData(6, ref nZ)) { return; }

            // 3. If data is invalid, we need to abort.
            if (!cell.isValid) { return; }
            if (xCellSize == 0) { return; }
            if (yCellSize == 0) { return; }
            if (zCellSize == 0) { return; }
            if (nX == 0) { return; }
            if (nY == 0) { return; }
            if (nZ == 0) { return; }

            // 4. Initialise the lattice object
            var lattice = new Lattice();

            // 5. Prepare cell (this is a UnitCell object)
            cell = cell.Duplicate();
            cell.FormatTopology();
            
            // 6. Define BasePlane and directional iteration vectors
            Plane basePlane = Plane.WorldXY;
            Vector3d vectorX = xCellSize * basePlane.XAxis;
            Vector3d vectorY = yCellSize * basePlane.YAxis;
            Vector3d vectorZ = zCellSize * basePlane.ZAxis;

            float[] N = new float[3] { nX, nY, nZ };

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

                        // This loop maps each node in the cell
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
                                // Compute position vector
                                Vector3d V = uvw[0] * vectorX + uvw[1] * vectorY + uvw[2] * vectorZ;
                                // Instantiate new node
                                var newNode = new LatticeNode(basePlane.Origin + V);
                                // Add new node to tree
                                nodeList.Add(newNode);
                            }
                        }
                    }
                }
            }

            // 8. Map struts to the node tree
            lattice.ConformMapping(cell, N);

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
                //return Resources.circle1;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{3d9572a6-0783-4885-9b11-df464cf549a7}"); }
        }
    }
}
