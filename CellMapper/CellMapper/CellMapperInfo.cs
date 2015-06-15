using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace CellMapper
{
    public class CellMapperInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "CellMapper";
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
                return new Guid("687cf5a2-cbdc-40eb-af14-037b9fe4eabe");
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
