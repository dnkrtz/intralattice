using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GridCylinder
{
    public class GridCylinderInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "GridCylinder";
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
                return new Guid("cfb5f521-d326-421f-bc15-0d19e9501cdc");
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
