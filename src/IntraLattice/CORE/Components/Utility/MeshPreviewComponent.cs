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
    public class MeshPreviewComponent : GH_Component
    {
        // Mesh for previewing, declared at class level
        private List<Mesh> m_mesh = new List<Mesh>();

        /// <summary>
        /// Initializes a new instance of the MeshPreviewComponent class.
        /// </summary>
        public MeshPreviewComponent()
            : base("Mesh Preview", "MeshPreview",
                "Generates a preview of the mesh.",
                "IntraLattice2", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh(es) to preview.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Retrieve and validate input
            var mesh = new List<Mesh>();
            if (!DA.GetDataList(0, mesh)) { return; }
            if (mesh == null || mesh.Count == 0) { return; }

            m_mesh = mesh; // for preview (see DrawViewPortMeshes method below)
        }

        /// <summary>
        /// Override default preview behaviour (mesh and wire colors)
        /// </summary>
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(Color.FromArgb(255, 255, 255), 0);

            base.DrawViewportMeshes(args);
            base.DrawViewportWires(args);

            if (m_mesh != null)
            {
                foreach (Mesh mesh in m_mesh)
                {
                    if (mesh != null && mesh.IsValid)
                    {
                        args.Display.DrawMeshShaded(mesh, mat);
                        args.Display.DrawMeshWires(mesh, Color.Black);
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
                return Resources.meshPreview;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{c5e3b143-5534-4ad3-a711-33881772d683}"); }
        }
    }
}
