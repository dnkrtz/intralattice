using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using IntraLattice.Properties;
// This component generate a random lattice frame according to input Brep and Random number
// ============================================================
// 
// Random algorithm changing the input lattice frame into a randomized one
// Output frame list and node list
//
// Version:0.0.1
// Written by Yunlong Tang (http://adml.lab.mcgill.ca/)
// CopyRight ADML lab McGill
namespace IntraLattice
{
    // Line Structure consists node
    public class FrameRandom : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the FrameRandom class.
        /// </summary>
        public FrameRandom()
            : base("FrameRandom", "Fr_Random",
                "Generate random uniform frame",
                "IntraLattice2", "Frame")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("LatticeFrame", "Frame", "frame of generated lattice", GH_ParamAccess.list);
            pManager.AddNumberParameter("Randomize_Intensity", "Intensity", "Intensity of randomization 0-10", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tolerance", "T", "Tolerance of node", GH_ParamAccess.item, 0.01);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("LatticeFrame","Frame","frame of randomized lattice",GH_ParamAccess.list);
            pManager.AddPointParameter("NodePosition","Node","node of randomized lattice",GH_ParamAccess.list);
            pManager.AddIntegerParameter("StartNode","SNode","Start node of randomized lattice",GH_ParamAccess.list);
            pManager.AddIntegerParameter("EndNode","ENode","End node of randomized lattice",GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            // Initialize list for node and line
            List<Line> listOfFrame = new List<Line>();
            List<Point3d> listOfNode = new List<Point3d>();
            listOfNode.Clear();
            listOfFrame.Clear();

            // Initialize start node and end node position list
            List<int> listOfSNode = new List<int>();
            List<int> listOfENode = new List<int>();
            listOfENode.Clear();
            listOfENode.Clear();
            // The shortest distance of each node connected line
            List<double> listOfSDistance = new List<double>();
            listOfSDistance.Clear();

            // Initialize output new nodelist

            List<Point3d> oListOfNode = new List<Point3d>();
            oListOfNode.Clear();

            double tolerance = 0.01;
            double rIntensity = -1;
            
            // Get input
            DA.GetDataList(0, listOfFrame);
            DA.GetData(1,ref rIntensity);
            DA.GetData(2,ref tolerance);
            
            // Validate input
            if (rIntensity<0 || listOfFrame.Count<=0)
            {
                System.Exception ex = new System.Exception("Input invalid");
                throw (ex);
            }

            

            // Iterate line to construct node relationship
            for (int iOL = 0; iOL < listOfFrame.Count; iOL++)
            {
                Line pCurrentLine = listOfFrame[iOL];
                double lineLength = pCurrentLine.Length;
                Point3d sPoint = pCurrentLine.From;
                Point3d ePoint = pCurrentLine.To;
                int sPosition = -1;
                int ePosition = -1;
                for (int iON = 0; iON < listOfNode.Count; iON++ )
                {
                    Point3d pCNode = listOfNode[iON];
                    double pCShortLength = listOfSDistance[iON];
                    if(pCNode.DistanceTo(sPoint)<tolerance)
                    {
                        sPosition = iON;
                    }
                    if(pCNode.DistanceTo(ePoint)<tolerance)
                    {
                        ePosition = iON;
                    }
                }
                if (sPosition >= 0)  //Point is already in the list
                {
                    listOfSNode.Add(sPosition + 1);
                    if(lineLength<listOfSDistance[sPosition])
                    {
                        listOfSDistance[sPosition] = lineLength;
                    }
                }
                else
                {
                    //Add new point
                    listOfNode.Add(sPoint);
                    listOfSDistance.Add(lineLength);
                    listOfSNode.Add(listOfNode.Count);
                }
                if (ePosition >= 0)  //Point is already in the list
                {
                    listOfENode.Add(ePosition + 1);
                    if(lineLength<listOfSDistance[ePosition])
                    {
                        listOfSDistance[ePosition] = lineLength;
                    }
                }
                else
                {
                    // Add new point
                    listOfNode.Add(ePoint);
                    listOfSDistance.Add(lineLength);
                    listOfENode.Add(listOfNode.Count);
                }
            }// Organize line end

            // Randonmized node list

            // Initialize random class
            Random pRandom = new Random();

            for (int iON = 0; iON < listOfNode.Count; iON++ )
            {
                Point3d pCNode = listOfNode[iON];
                double ShortLineLength = listOfSDistance[iON];

                // Build a random vector
                double vX = pRandom.NextDouble();
                double vY = pRandom.NextDouble();
                double vZ = pRandom.NextDouble();
                Vector3d pVector = new Vector3d(vX,vY,vZ);
                // Unitize vector
                pVector.Unitize();
                double distance = pRandom.NextDouble();
                double lengthOfVector = ((ShortLineLength ) * rIntensity / 10) * distance;
                Vector3d tVector = lengthOfVector * pVector;
                Point3d pNewPoint = new Point3d(pCNode.X+tVector.X,pCNode.Y+tVector.Y,pCNode.Z+tVector.Z);
                oListOfNode.Add(pNewPoint);
            }

            // build output frame
            List<Line> oNewFrame = new List<Line>();
            oNewFrame.Clear();
            for(int iOL = 0; iOL<listOfFrame.Count;iOL++)
            {
                int sPosition = listOfSNode[iOL]-1;
                Point3d sPoint = oListOfNode[sPosition];
                int ePosition = listOfENode[iOL]-1;
                Point3d ePoint = oListOfNode[ePosition];
                Line pNewLine = new Line(sPoint,ePoint);
                oNewFrame.Add(pNewLine);
            }
            DA.SetDataList(0,oNewFrame);
            DA.SetDataList(1, oListOfNode);
            DA.SetDataList(2, listOfSNode);
            DA.SetDataList(3, listOfENode);



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
                return Resources.Randomize;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{6e4d876d-b8f1-44a5-9d6a-01a7047c7e2b}"); }
        }
    }
}