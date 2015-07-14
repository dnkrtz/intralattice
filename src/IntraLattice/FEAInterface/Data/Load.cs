using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.FEAInterface.Data
{
    // This is a class to represent external load
    class Load
    {
        private string m_LoadType;
        private string m_NodePosition;
        private string m_MagN1;
        private string m_MagN2;
        private string m_MagN3;
        private string m_LoadCase;
        private string m_LoadCord;
        private string m_LoadScale;
        // Construct Function
        public Load(string iLoad)
        {
            // Load Format / LoadType, LoadSetID, NodeID, Cord, Scale, N1,N2,N3;
            // Define split char
            if (iLoad==null)
            {
                throw new ArgumentNullException("Load type receive null string");
            }
            // split the string according to ","
            char[] delimiterChar = {','};
            string[] keywords = iLoad.Split(delimiterChar);
            if (keywords.Length<8)
            {
                throw new ArgumentException("Not enough input parameters of load string");
            }
            m_LoadType = keywords[0];
            m_LoadCase = keywords[1];
            m_NodePosition = keywords[2];
            m_LoadCord = keywords[3];
            m_LoadScale = keywords[4];
            m_MagN1 = keywords[5];
            m_MagN2 = keywords[6];
            m_MagN3 = keywords[7];  
        }

        // Get Load Information
        public string GetLoadType()
        {
            return m_LoadType;
        }
        public string GetNodePosition()
        {
            return m_NodePosition;
        }
        public string GetMagN1()
        {
            return m_MagN1;
        }
        public string GetMagN2()
        {
            return m_MagN2;
        }
        public string GetMagN3()
        {
            return m_MagN3;
        }
        public string GetLoadCase()
        {
            return m_LoadCase;
        }
        public string GetLoadCord()
        {
            return m_LoadCord;
        }
        public string GetLoadScale()
        {
            return m_LoadScale;
        }
    }
}
