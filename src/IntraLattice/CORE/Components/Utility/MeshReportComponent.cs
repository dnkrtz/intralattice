using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using IntraLattice.Properties;
using System.Drawing;

// Summary:     This component is a post-processing tool used to inspect a mesh.
// ===============================================================================
// Details:     - Checks that the mesh represents a solid, and returns a comprehensive report.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.UtilityModule
{
    public class MeshReportComponent : GH_Component
    {
        // Naked edges for previewing, declared at class level
        private Polyline[] m_nakedEdges;

        /// <summary>
        /// Initializes a new instance of the MeshReportComponent class.
        /// </summary>
        public MeshReportComponent()
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
            // 1. Retrieve/validate input
            Mesh mesh = null;
            if (!DA.GetData(0, ref mesh)) { return; }
            if (!mesh.IsValid) { return; }

            // 2. Initiliaze variables
            string report = "";     // report string
            bool isValid = true;    // will be false if any of the tests fail
            bool isOriented, hasBoundary;

            // 3. Check - naked edges
            report = "- Details -\n";

            m_nakedEdges = mesh.GetNakedEdges();

            if (m_nakedEdges == null)
            {
                report += "Mesh has 0 naked edges. \n";
            }
            else
            {
                report += String.Format("Mesh has {0} naked edges. \n", m_nakedEdges.Length);
                isValid = false;
            }

            // 4. Check - manifoldness
            if (mesh.IsManifold(true, out isOriented, out hasBoundary))
            {
                report += "Mesh is manifold. \n";
            }
            else
            {
                report += "Mesh is non-manifold. \n";
                isValid = false;
            }

            // 5. Check - mesh orientation
            if (mesh.SolidOrientation() == 1)
            {
                report += "Mesh is solid. \n";
            }
            else if (mesh.SolidOrientation() == 0)
            {
                report += "Mesh is not solid. \n";
                isValid = false;
            }
            // Inward facing normals
            else
            {
                mesh.Flip(true, true, true);
                report += "Mesh is solid. (normals have been flipped) \n";
            }

            // 6. Finally, summarize these results
            if (isValid)
            {
                report = "Mesh is VALID.\n\n" + report;
            }
            else
            {
                report = "Mesh is INVALID.\n\n" + report;
            }

            // 7. Add title
            report = "- Overview -\n" + report;

            // 8. Output report
            DA.SetData(0, report);
        }

        /// <summary>
        /// Display naked edges
        /// </summary>
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (m_nakedEdges != null)
            {
                foreach (Polyline nakedEdge in m_nakedEdges)
                {
                    if (nakedEdge.IsValid)
                    {
                        args.Display.DrawPolyline(nakedEdge, Color.DarkRed);
                    }
                }
            }
            
        }

        /// <summary>
        /// Sets the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.tertiary;
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.meshReport;
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