using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace PrimitiveBox
{
    public class PrimitiveBoxInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "PrimitiveBox";
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
                return new Guid("ef7417d4-7498-4d7e-9f33-835d6f02f881");
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
