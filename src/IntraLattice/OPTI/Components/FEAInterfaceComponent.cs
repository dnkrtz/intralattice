using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using IntraLattice.OPTI.Data;
using IntraLattice.OPTI.Manager;


namespace IntraLattice.OPTI.Components
{
    // Define the type of crossection
    public class FEAInterfaceComponent : GH_Component
    {
        /// </Title>
        /// Author: Yunlong Tang
        /// Main Function:This is the GH component to build 
        ///               Nastran FEA file based on lattice frame and selected boundary condition
        /// Version:0.0.1
        /// </Title>
      
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        ///
        // List of Loads
        private List<Load> m_LoadList;

        // List Of SPC
        private List<SPC> m_SPCList;

        //List Of Node
        private List<Point3d> m_ListOfNode;

        //List of Beam
        private List<BeamElement> m_ListOfBeam;

        // List Of BeamCrossection

        private List<BeamCrossection> m_ListOfBeamCrossection;

        private List<MAT1> m_ListOfMaterial;

        public FEAInterfaceComponent()
            : base("FEA_Interface_Nastran", "FEA/Nastran",
                "Generate Nastran File for FEA analysis",
                "IntraLattice2", "Optimization")
        {
            // Temporarily initialize the material
            m_ListOfMaterial = new List<MAT1>();
            m_ListOfBeam = new List<BeamElement>();
            m_ListOfNode = new List<Point3d>();
            m_ListOfBeamCrossection = new List<BeamCrossection>();
            m_LoadList = new List<Load>();
            m_SPCList = new List<SPC>();
            m_ListOfMaterial = new List<MAT1>();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddLineParameter("L_Frame","LF","Lines list of lattice frame",GH_ParamAccess.list);
            pManager.AddNumberParameter("Strut_Thickness","S_T","List of strut thickness",GH_ParamAccess.list);
            pManager.AddTextParameter("Support","Sp","The support of FEA",GH_ParamAccess.list);
            pManager.AddTextParameter("Load","Ld","The Load of FEA",GH_ParamAccess.list);
            pManager.AddTextParameter("Crossection", "CS", "Struts Crossection", GH_ParamAccess.item);
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("NastranFile", "NF", "The nastran script output", GH_ParamAccess.item);
            pManager.AddPointParameter("NodePosition", "NP", "The Node Position", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Empty Old Case
            m_ListOfMaterial.Clear();
            m_ListOfNode.Clear();
            m_LoadList.Clear();
            m_SPCList.Clear();
            m_ListOfBeam.Clear();
            m_ListOfBeamCrossection.Clear();

            // Add Material
            MAT1 DefaultMat = new MAT1(1, 74000, 0.338);
            m_ListOfMaterial.Add(DefaultMat);


            // Read Input
            // Define List of line as frame
            List<Line> ListOfLine = new List<Line>();
            DA.GetDataList(0, ListOfLine);
            // Define List of thickness
            List<double> ListOfThickness = new List<double>();
            DA.GetDataList(1, ListOfThickness);
            // input validate
            if (ListOfThickness.Count!= ListOfLine.Count)
            {
                System.Exception ex = new System.Exception("Thickness count is not equal to line count");
                throw (ex);
            }

            // Define List of load
            List<string> ListOfLoad = new List<string>();
            DA.GetDataList(3, ListOfLoad);
            ReadLoad(ListOfLoad);


            // Define List of SPC
            List<string> ListOfSPC = new List<string>();
            DA.GetDataList(2, ListOfSPC);
            ReadSPC(ListOfSPC);

            // Define Crossection Type
            string CrossSection = "";
            DA.GetData(4, ref CrossSection);
            CPType CrossSectionType = BeamCrossection.StringtoCPType(CrossSection);

            // Read Element
            ReadElement(ListOfLine, ListOfThickness, ListOfThickness, CrossSectionType);


            // Define List of strut radius

            // Get Temporary File Path
            string  CurrentDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Add File Name to temporary File path 
            // Here, Multiple Component should be considered to avoid File name conflic
            string FilePathName = CurrentDirectory + "\\nastran.txt";

            // Build File Manager

            try
            {
                FileManager pFileManager = new FileManager(FilePathName);
                //pFileManager.WriteSubSectionTitle("STATIC SOLUTION");
                pFileManager.WriteTitle();
                pFileManager.WriteBeginBulk();
                // Write Node Position
                pFileManager.WriteNodePosition(m_ListOfNode);
                // Write BeamElement
                pFileManager.WriteBeamElement(m_ListOfBeam);
                // Write Beam Crossection
                pFileManager.WriteBeamSections(m_ListOfBeamCrossection);
                // Write Material
                pFileManager.WriteMaterials(m_ListOfMaterial);
                pFileManager.WriteSPC(m_SPCList);
                pFileManager.WriteLoads(m_LoadList);
                pFileManager.WriteEndData();

                pFileManager.WriteEnd();
            }
            catch (System.Exception ex)
            {
                DA.SetData(0, ex.ToString());
                DA.SetDataList(0, null);
                return;
            }
            string text = System.IO.File.ReadAllText(@FilePathName);
            DA.SetData(0, text);
            List<GH_Point> OutputPoint = new List<GH_Point>();
            OutputPoint.Clear();
            for (int i = 0; i < m_ListOfNode.Count;i++ )
            {
                GH_Point pPoint = new GH_Point(m_ListOfNode[i]);
                OutputPoint.Add(pPoint);
            }
            DA.SetDataList(1, OutputPoint);
        }

        /// <summary>
        /// Here we set the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
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
            get { return new Guid("{353c9115-fc91-43f8-9877-e24acfd98a63}"); }
        }

        // This function is used to build node and beam element
        private void ReadElement(List<Line>iListOfLine,List<double> iListOfBeginThickness, List<double> iListOfEndThickness, CPType iCrossectionType)
        {
            // iterate line to find node first, if node not existing and build node
            // Merge Tolerance set as
            double MergeT = 1e-3;
            for (int IoL = 0; IoL<iListOfLine.Count;IoL++)
            {
                Line CLine = iListOfLine[IoL];
                Point3d StartP = CLine.From;
                Point3d EndP = CLine.To;
                int StartPosition = -1;
                int EndPosition = -1;
                // find exact the same node
                // Be careful node number and node position has 1 deference
                for (int IoN = 0; IoN<m_ListOfNode.Count; IoN++)
                {
                    Point3d CNode = m_ListOfNode[IoN];
                    if (StartP.DistanceTo(CNode)<MergeT)
                    {
                        StartPosition = IoN;
                    }
                    if (EndP.DistanceTo(CNode)<MergeT)
                    {
                        EndPosition = IoN;
                    }
                }
                //Create a new beam element
                //Beam element number count
                int BeamCount = m_ListOfBeam.Count;
                BeamElement BElement = new BeamElement(BeamCount + 1);
                if (StartPosition!=-1)
                {
                    BElement.BeginNode = StartPosition+1;
                }
                else
                {
                    m_ListOfNode.Add(StartP);
                    BElement.BeginNode = m_ListOfNode.Count;
                }
                if (EndPosition!=-1)
                {
                    BElement.EndNode = EndPosition +1;
                }
                else
                {
                    m_ListOfNode.Add(EndP);
                    BElement.EndNode = m_ListOfNode.Count;
                }

                // Calculate orientation vector
                Vector3d UnitZ = new Vector3d(0.0,0.0,1.0);
                double x = EndP.X - StartP.X;
                double y = EndP.Y - StartP.Y;
                double z = EndP.Z - StartP.Z;
                Vector3d BeamV = new Vector3d(x, y, z);
                Vector3d OVector = Vector3d.CrossProduct(BeamV, UnitZ);
                if (BeamV.IsParallelTo(UnitZ)!=0)
                {
                    OVector = new Vector3d(1.0, 0.0, 0.0);
                }
                OVector.Unitize();
                BElement.Orientation_x = OVector.X;
                BElement.Orientation_y = OVector.Y;
                BElement.Orientation_z = OVector.Z;

                // Define the Beam Crossection Properties
                int BeamCrossectionCount = m_ListOfBeamCrossection.Count;

                // TEMP CODE Begin

                // TEMP TO GET MATERIAL

                int MatID = m_ListOfMaterial[0].MatID;

                // TEMP CODE END

                List<double> StartDiameter = new  List<double>();
                StartDiameter.Add(iListOfBeginThickness[IoL]);

                List<double> EndDiamter = new List<double>();
                EndDiamter.Add(iListOfEndThickness[IoL]);

                BeamCrossection BCross = new BeamCrossection(CPType.ROD, BeamCrossectionCount + 1, MatID,StartDiameter,EndDiamter,0);
                m_ListOfBeamCrossection.Add(BCross);
                BElement.PropertyNum = BeamCrossectionCount + 1;

                m_ListOfBeam.Add(BElement);

            }
        
        }

        // This function is used to read load and based on load to generate load vector
        private void ReadLoad(List<string> iListOfLoad)
        {
            // Load Format / LoadType, LoadSetID, NodeID, Cord, Scale, N1,N2,N3;
                             // Define split char
            for (int IoL=0; IoL<iListOfLoad.Count; IoL++ )
            {
                Load pLoad = new Load(iListOfLoad[IoL]);
                m_LoadList.Add(pLoad);
            }
        }

        // This function is used to read SPC
        private void ReadSPC(List<string> iListOfSPC)
        {
            // SPC Format
            for (int IoS = 0; IoS < iListOfSPC.Count; IoS++)
            {
                SPC pSPC = new SPC(iListOfSPC[IoS]);
                m_SPCList.Add(pSPC);
            }

        }
    }
}
