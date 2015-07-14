using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace IntraLattice.FEAInterface.Components
{
    public class PointLoadComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public PointLoadComponent()
            : base("PointLoad", "PLoad",
                "Generate Load script of FEA analysis",
                "IntraLattice2", "FEA")
        {
            m_ListOfLoadPoint = new List<GH_Point>();
            m_ListOfNode = new List<GH_Point>();
            m_LoadVector = new Vector3d();
        }

        private List<GH_Point> m_ListOfNode;
        private List<GH_Point> m_ListOfLoadPoint;
        Vector3d m_LoadVector;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("NodePosition", "Node", "NodeOfFEA", GH_ParamAccess.list);
            pManager.AddPointParameter("LoadPosition", "LoadPoint", "Point of Load", GH_ParamAccess.list);
            pManager.AddVectorParameter("ForceVector", "ForceV", "MagnituteOfForce", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("LoadScript", "LOAD", "Load script for analysis", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Get Input
            m_ListOfNode.Clear();
            m_ListOfLoadPoint.Clear();
            DA.GetDataList(0, m_ListOfNode);
            DA.GetDataList(1, m_ListOfLoadPoint);
            DA.GetData(2, ref m_LoadVector);
            List<string> OutputString = new List<string>();
            string sVector = m_LoadVector.ToString();
            for (int IoL = 0; IoL < m_ListOfLoadPoint.Count;IoL++)
            {
                string LoadString = "FORCE,1,";
                // Find Node Position
                int NodeID = -1;
                for(int IoN = 0; IoN<m_ListOfNode.Count;IoN++)
                {
                    if (m_ListOfNode[IoN].QC_Distance(m_ListOfLoadPoint[IoL])<0.01)
                    {
                        NodeID = IoN;
                        break;
                    }
                }
                if (NodeID<0)
                {
                    continue;
                }
                NodeID = NodeID + 1;
                LoadString = LoadString + NodeID.ToString();
                LoadString = LoadString + ",0,1.0,";
                LoadString = LoadString + sVector;
                OutputString.Add(LoadString);
            }
            DA.SetDataList(0,OutputString);

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
            get { return new Guid("{7399e90a-b422-4b90-8b88-146aae0191ab}"); }
        }
    }
}
