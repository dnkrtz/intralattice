using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using IntraLattice.FEAInterface.Data;

namespace IntraLattice.FEAInterface.Manager
{
    // Author:TYL
    // This class is use to write Text File
    class FileManager
    {
        private string m_FilePath;
        private System.IO.StreamWriter m_File;

        // Construct the class 
        public FileManager(string iFilePath)
        {
            if (iFilePath!=null)
            {
                m_FilePath = iFilePath;
                try
                {
                    m_File = new System.IO.StreamWriter(@m_FilePath);
                }
                catch (System.Exception ex)
                {
                    throw ex;
                }
                
            }
            else
            {
                throw new ArgumentNullException();
            }
        }


        // Write Line
        public void WriteLine(string iLine)
        {
            m_File.WriteLine(iLine);
        }

        //Close streanwriter
        public void WriteEnd()
        {
            m_File.Close(); 
        }

        // Write subsection Tilte
        public void WriteSubSectionTitle(string SubsectionName)
        {
            WriteLine("$ ");
            WriteLine("$ " + SubsectionName);
            WriteLine("$ ");
        }

        public void WriteNodePosition(List<Point3d> iListOfNode)
        {
            WriteSubSectionTitle("NODAL COORDINATES");
            string sGrid = "GRID*";
            for (int IoN = 0; IoN <iListOfNode.Count;IoN++ )
            {
                // Write Grid /Node
                string WriteString = string.Format("{0,-8}", sGrid);

                WriteString = WriteString + string.Format("{0,16}", IoN + 1);

                WriteString = WriteString + string.Format("{0,16}", "");

                string XCord = string.Format("{0:0.00000000e+000}", iListOfNode[IoN].X);

                WriteString = WriteString + string.Format("{0,16}",XCord);

                string YCord = string.Format("{0:0.00000000e+000}", iListOfNode[IoN].Y);

                WriteString = WriteString + string.Format("{0,16}", YCord);

                WriteLine(WriteString);

                WriteString = string.Format("{0,-8}", "*");

                string ZCord = string.Format("{0:0.00000000e+000}", iListOfNode[IoN].Z);

                WriteString = WriteString + string.Format("{0,16}", ZCord);

                WriteLine(WriteString);

                

                
            }



        }

        public void WriteBeamElement(List<BeamElement> iListOfElement)
        {
            WriteSubSectionTitle("BEAM ELEMENTS");
            string sBeamElement = "CBEAM";
            for (int IoE = 0; IoE < iListOfElement.Count; IoE++)
            {
                //Write String
                string WriteString = string.Format("{0,-8}", sBeamElement);
                // BeamID
                WriteString = WriteString + string.Format("{0,8}", iListOfElement[IoE].Num);
                // PropertyID
                WriteString = WriteString + string.Format("{0,8}", iListOfElement[IoE].PropertyNum);
                // Begin Node
                WriteString = WriteString + string.Format("{0,8}", iListOfElement[IoE].BeginNode);
                // End Node
                WriteString = WriteString + string.Format("{0,8}", iListOfElement[IoE].EndNode);
                // Orientation x
                string XOrientation = string.Format("{0:0.000}", iListOfElement[IoE].Orientation_x);
                WriteString = WriteString + string.Format("{0,8}", XOrientation);
                // Orientation y
                string YOrientation = string.Format("{0:0.000}", iListOfElement[IoE].Orientation_y);
                WriteString = WriteString + string.Format("{0,8}", YOrientation);
                // Orientation z
                string ZOrientation = string.Format("{0:0.000}", iListOfElement[IoE].Orientation_z);
                WriteString = WriteString + string.Format("{0,8}", ZOrientation);

                WriteLine(WriteString);

            }

        }

        public void WriteBeamSections(List<BeamCrossection> iListOfBeamCrossection)
        {
            int LineNumber = 8;
            string LineConnect = "+z";
            string sBeam = "PBEAML";
            string sEmpty = "";
            WriteSubSectionTitle("BEAM SECTIONS");
            for (int IoB = 0; IoB < iListOfBeamCrossection.Count;IoB++)
            {
                // Write First line

                string WriteString = string.Format("{0,-8}", sBeam);
                // Property ID
                WriteString = WriteString + string.Format("{0,8}", iListOfBeamCrossection[IoB].PID);
                // Material ID
                WriteString = WriteString + string.Format("{0,8}", iListOfBeamCrossection[IoB].MID);
                // Group
                WriteString = WriteString + string.Format("{0,8}", sEmpty);
                // Type
                string TypeString = BeamCrossection.CPTypeToString(iListOfBeamCrossection[IoB].Type);
                WriteString = WriteString + string.Format("{0,8}", TypeString);

                // Emptye6
                WriteString = WriteString + string.Format("{0,8}", sEmpty);

                // Emptye7
                WriteString = WriteString + string.Format("{0,8}", sEmpty);

                // Emptye8
                WriteString = WriteString + string.Format("{0,8}", sEmpty);

                // Emptye9
                WriteString = WriteString + string.Format("{0,8}", sEmpty);

                string connectString = LineConnect+LineNumber.ToString();

                // Connect 10

                WriteString = WriteString + string.Format("{0,-8}", connectString);

                WriteLine(WriteString);

                LineNumber++;

                // Begin Second Line

                WriteString = string.Format("{0,-8}", connectString);

                // Write Dimension
                List<double> Dimension = iListOfBeamCrossection[IoB].DIM_A;
                for (int IoD = 0; IoD < Dimension.Count; IoD++)
                {
                    string DimString = string.Format("{0:0.0}", Dimension[IoD]);
                    WriteString = WriteString + string.Format("{0,8}", DimString);
                }

                // Non structural mass per mm its empty
                WriteString = WriteString + string.Format("{0,8}", sEmpty);

                // Write output stress sign
                WriteString = WriteString + string.Format("{0,8}", "YES");

                // Write total length

                WriteString = WriteString + string.Format("{0,8}", "1.0");


                // Dimension B
                Dimension = iListOfBeamCrossection[IoB].DIM_B;
                for (int IoD = 0; IoD < Dimension.Count; IoD++)
                {
                    string DimString = string.Format("{0:0.0}", Dimension[IoD]);
                    WriteString = WriteString + string.Format("{0,8}", DimString);
                }
                // Non structural mass per mm its empty
                WriteString = WriteString + string.Format("{0,8}", sEmpty);

                WriteLine(WriteString);

            }
        }

        public void WriteMaterials(List<MAT1> iListOfMaterials)
        {
            WriteSubSectionTitle("MATERIALS");
            string sMAT = "MAT1";
            string sEmpty = "";
            for (int IoM = 0; IoM < iListOfMaterials.Count; IoM++ )
            {
                //Write String
                string WriteString = string.Format("{0,-8}", sMAT);

                WriteString = WriteString + string.Format("{0,8}", iListOfMaterials[IoM].MatID);


                string ElasticModulus = RealToString(iListOfMaterials[IoM].Emodulus);

                WriteString = WriteString + string.Format("{0,8}", ElasticModulus);

                WriteString = WriteString + string.Format("{0,8}", sEmpty);

                string PossionRatio = string.Format("{0:0.###}", iListOfMaterials[IoM].EPoRatio);

                WriteString = WriteString + string.Format("{0,8}", PossionRatio);

                WriteLine(WriteString);


            }




        }

        public void WriteLoads(List<Load> iListOfLoad)
        {

            WriteSubSectionTitle("LOADS");
            for (int IoL = 0; IoL < iListOfLoad.Count;  IoL++)
            {
                //Write String
                string WriteString = string.Format("{0,-8}", iListOfLoad[IoL].GetLoadType());

                WriteString = WriteString + string.Format("{0,8}", iListOfLoad[IoL].GetLoadCase());

                WriteString = WriteString + string.Format("{0,8}", iListOfLoad[IoL].GetNodePosition());

                WriteString = WriteString + string.Format("{0,8}", iListOfLoad[IoL].GetLoadCord());

                double loadScale = System.Convert.ToDouble(iListOfLoad[IoL].GetLoadScale());

                string sLoadScale = RealToString(loadScale);

                WriteString = WriteString + string.Format("{0,8}", sLoadScale);

                double MagN1 = System.Convert.ToDouble(iListOfLoad[IoL].GetMagN1());

                string sMagN1 = RealToString(MagN1);

                WriteString = WriteString + string.Format("{0,8}", sMagN1);

                double MagN2 = System.Convert.ToDouble(iListOfLoad[IoL].GetMagN2());

                string sMagN2 = RealToString(MagN2);

                WriteString = WriteString + string.Format("{0,8}", sMagN2);

                double MagN3 = System.Convert.ToDouble(iListOfLoad[IoL].GetMagN3());

                string sMagN3 = RealToString(MagN3);

                WriteString = WriteString + string.Format("{0,8}", sMagN3);

                WriteLine(WriteString);



            }

        }

        public void WriteSPC(List<SPC> iListOfSPC)
        {
            WriteSubSectionTitle("BOUNDARY CONDITIONS");
            for (int IoS = 0; IoS < iListOfSPC.Count; IoS++ )
            {
                // Write SPC TYPE
                string WriteString = string.Format("{0,-8}", iListOfSPC[IoS].GetSPCType());

                // Write SPC GROUP
                WriteString = WriteString + string.Format("{0,8}", iListOfSPC[IoS].GetLoadCase());

                // Write SPC CONSTRAINT
                WriteString = WriteString + string.Format("{0,8}", iListOfSPC[IoS].GetSPCContraints());

                // Write SPC NODE
                WriteString = WriteString + string.Format("{0,8}", iListOfSPC[IoS].GetNodeNumber());

                WriteLine(WriteString);

            }



        }

        public void WriteTitle()
        {
            WriteSubSectionTitle("STATIC SOLUTION");
            WriteLine("SOL LINEAR STATIC");
            WriteLine("$");
            WriteLine("TITLE = INTRALLATICE MODEL");
            WriteLine("$");
            WriteLine("DISPLACEMENT(PLOT) = ALL");
            WriteLine("ELSTRESS(PLOT,CORNER) = ALL");
            WriteLine("ELFORCE(PLOT,CORNER) = ALL");
            WriteSubSectionTitle("LOAD CASE DEFINITIONS");
            WriteLine("SUBCASE 1");
            WriteLine(" LABEL = LOAD_CASE_1");
            WriteLine(" SPC = 1");
            WriteLine(" LOAD = 1");
            WriteSubSectionTitle("COORDINATE SYSTEM");
            WriteLine("SET 1 = ALL");
            WriteLine("SURFACE 1, SET 1, SYSTEM BASIC, AXIS X, NORMAL Z");
        }

        public void WriteBeginBulk()
        {
            WriteLine("BEGIN BULK");
        }

        public void WriteEndData()
        {
            WriteLine("$");
            WriteLine("ENDDATA");
        }

        //This is a function to conver a real number to string in the format, if real number is integer it only show a decimal.Otherwise it will show all digit
        private string RealToString(double iNumber)
        {
            string str_a = string.Format("{0:F}", iNumber);
            int dot = str_a.IndexOf(".");
            bool hasnotzerochar = false;//记录是否小数点后存在不为0的字符
            for (int i = 0; i < str_a.Length; i++)
            {
                char a = '0';
                if (i > dot && str_a[i] != a)
                {
                    hasnotzerochar = true;
                }
            }

            if (hasnotzerochar)
            {
                return str_a;
            }
            else
            {
                str_a = string.Format("{0:F0}", iNumber);
                str_a = str_a + ".";
                return str_a;
            }
        }
    }
}
