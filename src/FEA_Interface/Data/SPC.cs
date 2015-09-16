using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FEA_Interface.Data
{
    //This Class is use to present Static Point Constraints
    class SPC
    {
        // Format Type,Case,Freedom,Node
        private string m_SPCType;
        private string m_SPCConstraints;
        private string m_SPCNodeNumber;
        private string m_LoadCase;
        public SPC(string iString)
        {
            if (iString==null)
            {
                throw new ArgumentNullException("SPC Input Null");
            }
            // split the string according to ","
            char[] delimiterChar = { ',' };
            string[] keywords = iString.Split(delimiterChar);
            if (keywords.Length<4)
            {
                throw new ArgumentException("SPC Input is invalid");
            }
            m_SPCType = keywords[0];
            m_LoadCase = keywords[1];
            m_SPCConstraints = keywords[2];
            m_SPCNodeNumber = keywords[3];
            
        }
        public string GetSPCType()
        {
            return m_SPCType;
        }
        public string GetSPCContraints()
        {
            return m_SPCConstraints;
        }
        public string GetNodeNumber()
        {
            return m_SPCNodeNumber;
        }
        public string GetLoadCase()
        {
            return m_LoadCase;
        }
    }
}
