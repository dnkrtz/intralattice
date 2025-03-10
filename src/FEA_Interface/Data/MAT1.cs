﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FEA_Interface.Data
{
    // This is the class of isotropic material properties
    class MAT1
    {
        // int Mat number

        private int m_MatID;
        public int MatID
        {
            get
            {
                return m_MatID;
            }
            set
            {
                m_MatID = value;
            }
        }

        // Young's modulus
        private double m_E;

        public double Emodulus
        {
            get
            {
                return m_E;
            }
        }

        //Possion Ratio
        private double m_PoRatio;

        public double EPoRatio
        {
            get
            {
                return m_PoRatio;
            }
        }

        //Density
        private double m_Density;

        public double Density
        {
            get
            {
                return m_Density;
            }
        }
        public MAT1(int iMatID, double iE, double iPoRatio, double iDensity)
        {
            m_MatID = iMatID;
            m_E = iE;
            m_PoRatio = iPoRatio;
            m_Density = iDensity;
        }
    }
}
