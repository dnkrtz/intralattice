using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using IntraLattice.CORE.Helpers;
using IntraLattice.Properties;
using Rhino.Collections;
using Rhino;

// Summary:     This component cleans a curve network.
// ===============================================================================
// Details:     - 
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components.Utility
{
    public class CleanNetworkComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CleanNetworkComponent class.
        /// </summary>
        public CleanNetworkComponent()
            : base("Clean Network", "CleanNetwork",
                "Removes duplicate curves from a network, within specified tolerance.",
                "IntraLattice2", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Strut network to clean.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "Tol", "Tolerance for combining nodes.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Cleaned curve network.", GH_ParamAccess.list);
            pManager.AddPointParameter("Nodes", "Nodes", "List of unique nodes.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("CurveStart", "I", "Index in 'Nodes' for the start of each curve in 'Struts'.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("CurveEnd", "J", "Index in 'Nodes' for the end of each curve in 'Struts'.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables
            var struts = new List<Curve>();
            double tol = 0.0;
            // 2. Attempt to retrieve input
            if (!DA.GetDataList(0, struts)) { return; }
            if (!DA.GetData(1, ref tol)) { return; }
            // 3. Validate input
            if (struts == null || struts.Count == 1) { return; }
            if (tol < 0) { return; }

            // 4. Call cleaning method
            var nodes = new Point3dList();
            var nodePairs = new List<IndexPair>();
            struts = FrameTools.CleanNetwork(struts, tol, out nodes, out nodePairs);

            // 5. Organize index lists
            var strutStart = new List<int>();
            var strutEnd = new List<int>();
            foreach (IndexPair nodePair in nodePairs)
            {
                strutStart.Add(nodePair.I);
                strutEnd.Add(nodePair.J);
            }

            // 6. Set output
            DA.SetDataList(0, struts);
            DA.SetDataList(1, nodes);
            DA.SetDataList(2, strutStart);
            DA.SetDataList(3, strutEnd);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.cleanNetwork;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{8b3a2f8c-3a76-4b19-84b9-f3eea80010ea}"); }
        }
    }
}