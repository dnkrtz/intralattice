using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.FEAInterface.Data
{
    enum CPType { ROD, TUBE, BOX, BAR, CROSS };
    //This is a interface to generate crossection property of beam element
    class BeamCrossection
    {
        private CPType m_BeamCPType;
        public CPType Type
        {
            get
            {
                return m_BeamCPType;

            }
        }

        private int m_PID;

        public int PID
        {
            get
            {
                return m_PID;
            }
            set
            {
                if (value>0)
                {
                    m_PID = value;
                }
            }
        }

        private int m_MID;

        public int MID
        {
            get
            {
                return m_MID;
            }
            set
            {
                if (value>0)
                {
                    m_MID = value;
                }
            }
        }

        // Dimension at crossection A
        private List<double> m_DIM_A;

        public List<double> DIM_A
        {
            get
            {
                return m_DIM_A;
            }
            set
            {
                m_DIM_A = value;
            }
        }

        // Dimension at crossection B
        private List<double> m_DIM_B;
        public List<double> DIM_B
        {
            get
            {
                return m_DIM_B;
            }
            set
            {
                m_DIM_B = value;
            }
        }

        // Non structural mass per unit
        private double m_NOS;

        





        public BeamCrossection(CPType iPBeamType, int iPID, int iMID, List<double> iDIM_A, List<double> iDIM_B, double iNOS)
        {
            m_BeamCPType = iPBeamType;
            m_PID = iPID;
            m_MID = iMID;
            m_DIM_A = iDIM_A;
            m_DIM_B = iDIM_B;
            m_NOS = iNOS;
        }
        public static string CPTypeToString( CPType iType)
        {
            string oString;
            switch (iType)
            {
                case CPType.ROD:
                    oString = "ROD";
                    break;
                case CPType.BAR:
                    oString = "BAR";
                    break;
                case CPType.BOX:
                    oString = "BOX";
                    break;
                case CPType.CROSS:
                    oString = "CROSS";
                    break;
                case CPType.TUBE:
                    oString = "TUBE";
                    break;
                default:
                    oString = "ROD";
                    break;
            }

            return oString;

        }

        public static CPType StringtoCPType(string iString)
        {
            if (iString.Equals("ROD"))
            {
               return CPType.ROD;
            }
            if (iString.Equals("BAR"))
            {
                return CPType.BAR;
            }
            else
            {
                return CPType.ROD;
            }
        }


    }



}
