using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace PrimitiveSphere
{
    public class PrimitiveSphereInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "PrimitiveSphere";
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
                return new Guid("78fa1a93-e56d-499d-83ed-54747f03a820");
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
