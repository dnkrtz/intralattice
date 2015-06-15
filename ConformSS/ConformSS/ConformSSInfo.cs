using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ConformSS
{
    public class ConformSSInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "ConformSS";
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
                return new Guid("712c97ea-9c98-42de-8b49-39f987fc8359");
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
