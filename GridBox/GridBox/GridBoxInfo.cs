using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GridBox
{
    public class GridBoxInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "GridBox";
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
                return new Guid("e1f868c2-c9b4-40b0-b5bd-f66a5d0de457");
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
