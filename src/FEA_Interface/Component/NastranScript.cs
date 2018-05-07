using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FEA_Interface.Data;
using FEA_Interface.Manager;

namespace FEA_Interface.Component
{
    public class NastranScript : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the NastranScript class.
        /// Written By Yunlong
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

        public NastranScript()
            : base("NastranScript", "FEA/Nastran",
                "Generate Nastran Script for Analysis",
                "FEA_Interface", "ScriptGenerator")
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
            pManager.AddPointParameter("NodePosition","Node","the nodes list from lattice frame",GH_ParamAccess.list);
            pManager.AddIntegerParameter("StartNode", "S_Node", "Index of start node in the list", GH_ParamAccess.list);
            pManager.AddIntegerParameter("EndNode", "E_Node", "Index of end node in the list", GH_ParamAccess.list);
            pManager.AddTextParameter("Crossection", "CS", "Crossection of struts shape", GH_ParamAccess.list);
            pManager.AddTextParameter("Material", "Mat", "Material of struts", GH_ParamAccess.list);
            pManager.AddTextParameter("Load", "Load", "Load condition", GH_ParamAccess.list);
            pManager.AddTextParameter("Support", "Support", "Support", GH_ParamAccess.list);
            // If material or crossection of struts are not specified, it will use the default ID=1 Mat and ID=1 crossection
            pManager.AddTextParameter("StrutsCrossectionID", "Cross_ID", "Assigned struts with Cross ID can be input from StrutCross Component", GH_ParamAccess.list,"0,0");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("NastranFile", "NF", "The nastran script output", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Empty Old Case
            m_ListOfMaterial.Clear();
            m_ListOfNode.Clear();
            m_LoadList.Clear();
            m_SPCList.Clear();
            m_ListOfBeam.Clear();
            m_ListOfBeamCrossection.Clear();
            // Read input
            if (!DA.GetDataList(0, m_ListOfNode)) { return ; }
            // Start Index and End Index
            List<int> listOfs_Index = new List<int>();
            List<int> listOfe_Index = new List<int>();
            if (!DA.GetDataList(1, listOfs_Index)) { return; }
            if (!DA.GetDataList(2, listOfe_Index)) { return; }

            List<string> iListOfCrossection = new List<string>();
            List<string> iListOfMaterial = new List<string>();
            List<string> iListOfLoad = new List<string>();
            List<string> iListOfSupport = new List<string>();
            if (!DA.GetDataList(3, iListOfCrossection)) { return; }
            if (!DA.GetDataList(4, iListOfMaterial)) { return; }
            if (!DA.GetDataList(5, iListOfLoad)) { return; }
            if (!DA.GetDataList(6, iListOfSupport)) { return; }

            List<string> iListOfStrutCroID = new List<string>();
            if (!DA.GetDataList(7, iListOfStrutCroID)) { return; }

            //First build material
            ReadMaterial(iListOfMaterial);

            //Then build Crossection
            ReadCross(iListOfCrossection);
            //Then build beam

            ReadBeam(m_ListOfNode, listOfs_Index, listOfe_Index, iListOfStrutCroID);
            //Then build Load

            ReadLoad(iListOfLoad);

            //Then build support

            ReadSPC(iListOfSupport);

            //At the end output the script


            // Define List of strut radius
            // Get Temporary File Path
            string CurrentDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            // Add File Name to temporary File path 
            // Here, Multiple Component should be considered to avoid File name conflict
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
                return Resource1.NastranScript.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{57C94EEA-ED67-42E9-86AA-CC2CC223F78C}"); }
        }

        private void ReadLoad(List<string> iListOfLoad)
        {
            // Load Format / LoadType, LoadSetID, NodeID, Cord, Scale, N1,N2,N3;
            // Define split char
            for (int IoL = 0; IoL < iListOfLoad.Count; IoL++)
            {
                Load pLoad = new Load(iListOfLoad[IoL]);
                m_LoadList.Add(pLoad);
            }
        }

        private void ReadSPC(List<string> iListOfSPC)
        {
            // SPC Format
            for (int IoS = 0; IoS < iListOfSPC.Count; IoS++)
            {
                SPC pSPC = new SPC(iListOfSPC[IoS]);
                m_SPCList.Add(pSPC);
            }

        }

        private void ReadMaterial(List<string> iListOfMaterial)
        {
            for(int IoM = 0; IoM<iListOfMaterial.Count; IoM++)
            {
                char[] delimiterChar = { ',' };
                string[] keywords = iListOfMaterial[IoM].Split(delimiterChar);
                if(keywords.Length<4)
                {
                    continue;
                }

                MAT1 newMat = new MAT1(System.Convert.ToInt32(keywords[0]),System.Convert.ToDouble(keywords[1]),System.Convert.ToDouble(keywords[2]),System.Convert.ToDouble(keywords[3]));
                m_ListOfMaterial.Add(newMat);
            }
            return;
        }

        private void ReadCross(List<string> iListOfCross)
        {
            for(int IoC = 0; IoC<iListOfCross.Count; IoC++)
            {
                char[] delimiterChar = { ',' };
                string[] keywords = iListOfCross[IoC].Split(delimiterChar);
                if(keywords.Length<5)
                {
                    continue;
                }
                double startDiameter = System.Convert.ToDouble(keywords[2]);
                double endDiameter = System.Convert.ToDouble(keywords[3]);
                List<double> startDimension = new List<double>();
                startDimension.Add(startDiameter);
                List<double> endDimension = new List<double>();
                endDimension.Add(endDiameter);
                int MatID = System.Convert.ToInt32(keywords[4]);
                // To check the type of the beam
                if(keywords[0].Equals("ROD"))
                {
                    BeamCrossection BCross = new BeamCrossection(CPType.ROD, System.Convert.ToInt32(keywords[1]), MatID, startDimension, endDimension, 0);
                    m_ListOfBeamCrossection.Add(BCross);
                }

            }
            return;
        }

        private void ReadBeam(List<Point3d> iListOfNodes, List<int> iListOfStartNodes, List<int> iListOfEndNodes, List<string> iListOfStrutCross)
        {
            // First check material and crossection ID exist
            if (m_ListOfBeamCrossection.Count==0 || m_ListOfMaterial.Count==0)
            {
                return;
            }
            // Check input
            if (iListOfStartNodes.Count != iListOfEndNodes.Count)
            {
                return;
            }
            // build strut

            for(int IoB = 0; IoB<iListOfStartNodes.Count; IoB++)
            {
                BeamElement BElement = new BeamElement(IoB + 1);
                BElement.BeginNode = iListOfStartNodes[IoB];
                BElement.EndNode = iListOfEndNodes[IoB];
                // Default PropertyNumber
                BElement.PropertyNum = 1;
                // Calculate orientation vector
                if(BElement.BeginNode>=iListOfNodes.Count||BElement.EndNode>=iListOfEndNodes.Count)
                {
                    continue;
                }
                Point3d StartP = iListOfNodes[BElement.BeginNode-1];
                Point3d EndP = iListOfNodes[BElement.EndNode-1];
                Vector3d UnitZ = new Vector3d(0.0, 0.0, 1.0);
                double x = EndP.X - StartP.X;
                double y = EndP.Y - StartP.Y;
                double z = EndP.Z - StartP.Z;
                Vector3d BeamV = new Vector3d(x, y, z);
                Vector3d OVector = Vector3d.CrossProduct(BeamV, UnitZ);
                if (BeamV.IsParallelTo(UnitZ) != 0)
                {
                    OVector = new Vector3d(1.0, 0.0, 0.0);
                }
                OVector.Unitize();
                BElement.Orientation_x = OVector.X;
                BElement.Orientation_y = OVector.Y;
                BElement.Orientation_z = OVector.Z;
                m_ListOfBeam.Add(BElement);
            }

            for (int IoC = 0; IoC < iListOfStrutCross.Count; IoC++)
            {
                string strutCross = iListOfStrutCross[IoC];
                char[] delimiterChar = { ',' };
                string[] keywords = strutCross.Split(delimiterChar);
                if(keywords.Length<2)
                {
                    continue;
                }


                int strutIndex = System.Convert.ToInt32(keywords[0]);
                int CrossID = System.Convert.ToInt32(keywords[1]);

                if (strutIndex== 0 || CrossID == 0)
                {
                    continue;
                }
                if(strutIndex<=m_ListOfBeam.Count)
                {
                    m_ListOfBeam[strutIndex - 1].PropertyNum = CrossID;
                }

            }
                return;
           
        }
    }
    
}