using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

// Summary:     This component is a post-processing tool used to inspect a mesh.
// ===============================================================================
// Details:     - Checks that the mesh represents a solid, and returns a comprehensive report.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.UtilityModule
{
    public class MeshReport : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MeshPreview class.
        /// </summary>
        public MeshReport()
            : base("Mesh Report", "MeshReport",
                "Verifies that the mesh represents a solid, and returns a comprehensive report.",
                "IntraLattice2", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh to inspect.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Report", "Report", "Report of inspection", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Set the mesh
            Mesh mesh = null;
            if (!DA.GetData(0, ref mesh)) { return; }
            if (!mesh.IsValid) { return; }

            string report = "";     // report string
            bool isValid = true;    // will be false if any of the tests fail
            bool isOriented, hasBoundary;

            // Check 1 - naked edges
            report = "- Details -\n";
            if (mesh.GetNakedEdges() == null)
                report += "Mesh has 0 naked edges. \n";
            else
            {
                report += String.Format("Mesh has {0} naked edges. \n", mesh.GetNakedEdges().Length);
                isValid = false;
            }

            // Check 2 - manifoldness
            if (mesh.IsManifold(true, out isOriented, out hasBoundary))
                report += "Mesh is manifold. \n";
            else
            {
                report += "Mesh is non-manifold. \n";
                isValid = false;
            }

            // Check 3 - mesh orientation
            if (mesh.SolidOrientation() == 1) report += "Mesh is solid. \n";
            else if (mesh.SolidOrientation() == 0)
            {
                report += "Mesh is not solid. \n";
                isValid = false;
            }
            else // inward facing normals
            {
                mesh.Flip(true, true, true);
                report += "Mesh is solid. (normals have been flipped) \n";
            }

            // Finally, summarize these results
            if (isValid)
                report = "Mesh is VALID.\n\n" + report;
            else
                report = "Mesh is INVALID.\n\n" + report;
            report = "- Overview -\n" + report;

            // Output report
            DA.SetData(0, report);
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{f49535d8-ab4a-4ee7-8721-290457b4e3eb}"); }
        }
    }
}