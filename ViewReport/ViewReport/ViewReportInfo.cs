using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace ViewReport
{
    public class ViewReportInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "ViewReport";
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
                return new Guid("ddd2d66e-8dba-4aa0-9a70-ade1a5c4fa9a");
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
