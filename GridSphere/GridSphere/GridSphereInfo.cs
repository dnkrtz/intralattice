using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GridSphere
{
    public class GridSphereInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "GridSphere";
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
                return new Guid("45fc64c9-cb9a-4518-a2ea-b2fa195d56a6");
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
