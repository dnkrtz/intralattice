using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using IntraLattice.Properties;
using System.Drawing;

// Summary:     This component is a post-processing tool used to preview a mesh.
// ===============================================================================
// Details:     - Overrides the default Grasshopper previewer to display a colored mesh and it's edges.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.UtilityModule
{
    public class MeshPreview : GH_Component
    {
        // Mesh for previewing, declared at class level
        private List<Mesh> m_mesh = new List<Mesh>();

        public MeshPreview()
            : base("Mesh Preview", "MeshPreview",
                "Verifies that the mesh represents a solid.",
                "IntraLattice2", "Utility")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh(es) to preview.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Set the mesh
            var mesh = new List<Mesh>();
            if (!DA.GetDataList(0, mesh)) { return; }
            if (mesh == null || mesh.Count == 0) { return; }

            m_mesh = mesh; // for preview (see DrawViewPortMeshes method below)
        }

        // Override default preview behaviour (mesh and wire colors)
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(Color.FromArgb(255, 255, 255), 0);

            base.DrawViewportMeshes(args);
            base.DrawViewportWires(args);

            foreach (Mesh mesh in m_mesh)
            {
                args.Display.DrawMeshShaded(mesh, mat);
                args.Display.DrawMeshWires(mesh, Color.Black);  
            }
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
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
