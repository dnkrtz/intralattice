using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FEA_Interface.Data
{
    // Author: TYL
    // This is the class to represent Beam element
    class BeamElement
    {
        private int m_Num;
        public int Num
        {
            get
            {
            return m_Num;
            }
            set
            {
                m_Num = value;
            }
        
        }
        private int m_BeginNode;
        public int BeginNode
        {
            get
            {
                return m_BeginNode;
            }
            set
            {
                m_BeginNode = value;
            }
        }
        private int m_EndNode;
        public int EndNode
        {
            get
            {
                return m_EndNode;
            }
            set
            {
                m_EndNode = value;
            }
        }
        
        private int m_PropertyNum;
        public int PropertyNum
        {
            get
            {
                return m_PropertyNum;
            }
            set
            {
                m_PropertyNum = value;
            }
        }
        private double m_Orientation_x;

        public double Orientation_x
        {
            get
            {
                return m_Orientation_x;
            }
            set
            {
                m_Orientation_x = value;
            }
        }
        private double m_Orientation_y;

        public double Orientation_y
        {
            get
            {
                return m_Orientation_y;
            }
            set
            {
                m_Orientation_y = value;
            }
        }
        private double m_Orientation_z;

        public double Orientation_z
        {
            get
            {
                return m_Orientation_z;
            }
            set
            {
                m_Orientation_z = value;
            }
        }

        public BeamElement(int iBeamNumber)
        {
            m_Num = iBeamNumber;
        }
    }
}
