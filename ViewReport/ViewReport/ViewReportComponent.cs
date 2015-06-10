using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ViewReport
{
    public class ViewReportComponent : GH_Component
    {
        public ViewReportComponent()
            : base("ViewReport", "ViewRep",
                "Verifies the validity of the mesh, and generates a preview",
                "IntraLattice2", "Meshing")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh to inspect", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Report", "R", "Report of inspection", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Set the mesh
            Mesh M = new Mesh();
            if (!DA.GetData(0, ref M)) { return; }
            if (M == null) { return; }

            String Report = "";

            // ADD PREVIEW HERE

            // Count naked edges
            int nakeds = M.GetNakedEdges().Length;
            Report += String.Format("Mesh has {0} naked edges. \n", nakeds);

            // Inspect mesh as solid
            if (M.SolidOrientation() == 1) Report += "Mesh is a solid. \n";
            else if (M.SolidOrientation() == 0) Report += "Mesh is NOT a solid. \n";
            else // inward facing normals
            {
                M.Flip(true,true,true);
                Report += "Mesh is a solid. (normals have been flipped) \n";
            }

            // Output report
            DA.SetData(0, Report);

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{c5e3b143-5534-4ad3-a711-33881772d683}"); }
        }
    }
}
