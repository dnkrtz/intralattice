using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace LatticeMesh
{
    public class LatticeMeshInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "LatticeMesh";
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
                return new Guid("548c3da6-c242-4198-ae39-c4b9632b6700");
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
