using System;
using System.Drawing;
using Grasshopper.Kernel;
using IntraLattice.FEAInterface.Data;
using IntraLattice.FEAInterface.Manager;

namespace IntraLattice.FEAInterface.Components
{
    public class FEAInterfaceInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "FEAInterface";
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
                return "This is the interface between lattice frame and FEA software";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("c90f26dd-837b-4631-a0e2-18f7c9f4e10e");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Yunlong Tang";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "tang.yunlong@mail.mcgill.ca";
            }
        }
    }
}
