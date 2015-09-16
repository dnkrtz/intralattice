using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace FEA_Interface
{
    public class FEA_InterfaceInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "FEAInterface";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "This is a set of component to do the FEA analysis for lattice structure";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("0245b72f-254f-4656-834b-07ab03631ef7");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Yunlong Tang @ McGill ADML lab";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "tang.yunlong@mail.mcgill.ca";
            }
        }
    }
}
