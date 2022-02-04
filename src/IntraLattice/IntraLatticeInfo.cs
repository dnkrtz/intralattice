using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace IntraLattice
{
    public class IntraLatticeInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "IntraLattice";
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
                return "IntraLattice plugin by Aidan Kurtz. Modified for compatibility with ShapeDiver Cloud Platform.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("df475ca3-9a35-471e-9348-f2b7c04e9189");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Aidan Kurtz";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "aidan.kurtz@mail.mcgill.ca";
            }
        }

        public override string Version => "0.7.7.1";
        public override string AssemblyVersion => this.Version;
    }
}
