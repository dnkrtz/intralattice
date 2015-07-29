using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using IntraLattice.Properties;
using System.Drawing;

// Summary:     This component is a post-processing tool used to inspect and preview a mesh
// ===============================================================================
// Details:     - Checks that the mesh represents a solid, and gives a comprehensive report.
//              - Overrides the default Grasshopper previewer to display a colored mesh and it's edges.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class MeshReport : GH_Component
    {
        // Mesh for previewing, declared at class level
        private Mesh m_mesh;

        public MeshReport()
            : base("Mesh Report", "MeshReport",
                "Verifies the validity of the mesh, and generates a preview",
                "IntraLattice2", "Utility")
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
            Mesh mesh = null;
            if (!DA.GetData(0, ref mesh)) { return; }
            if (!mesh.IsValid) { return; }

            m_mesh = mesh; // for preview (see DrawViewPortMeshes method below)

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

        // Override default preview behaviour (mesh and wire colors)
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(Color.FromArgb(255, 255, 255), 0);

            base.DrawViewportMeshes(args);
            base.DrawViewportWires(args);

            args.Display.DrawMeshShaded(m_mesh, mat);
            args.Display.DrawMeshWires(m_mesh, Color.Black);            
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.tertiary;
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.elec;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{c5e3b143-5534-4ad3-a711-33881772d683}"); }
        }
    }
}
