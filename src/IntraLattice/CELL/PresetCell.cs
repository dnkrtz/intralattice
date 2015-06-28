using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace IntraLattice
{
    public class PresetCell : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PresetCell class.
        /// </summary>
        public PresetCell()
            : base("PresetCell", "CellPreset",
                "Description",
                "IntraLattice2", "Cell")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Lines", "L", "Line topology", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_Line> lines = new List<GH_Line>();

            int topology = 0;

            for (int u=0; u<2; u++)
            {
                for (int v = 0; u < 2; u++)
                {
                    for (int w = 0; u < 2; u++)
                    {
                        // We'll be needing the data tree path of the current node, and those of its neighbours
                        GH_Path currentPath = new GH_Path(u, v, w);
                        List<GH_Path> neighbourPaths = new List<GH_Path>();

                        // Get neighbours!!
                        FrameTools.TopologyNeighbours(ref neighbourPaths, topology, new double[]{2,2,2}, u, v, w);

                        foreach (GH_Path neighbourPath in neighbourPaths) ;
                    }
                }
            }


            
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{508cc705-bc5b-42a9-8100-c1e364f3b83d}"); }
        }
    }
}