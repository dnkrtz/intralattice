using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ConformSA
{
    public class ConformSAInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "ConformSA";
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
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("5784a0d7-3fbe-4083-817f-1d721cba8aa6");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
