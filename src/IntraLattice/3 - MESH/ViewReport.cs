using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.DocObjects;

// This component is a post-processing tool used to inspect a mesh
// Checks that the mesh represents a solid

namespace IntraLattice
{
    public class ViewReport : GH_Component
    {
        public ViewReport()
            : base("ViewReport", "ViewReport",
                "Verifies the validity of the mesh, and generates a preview",
                "IntraLattice2", "Mesh")
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
            Mesh mesh = new Mesh();
            if (!DA.GetData(0, ref mesh)) { return; }
            if (mesh == null) { return; }

            String report = "";

            // ADD PREVIEW HERE

            // Count naked edges
            int nakeds = mesh.GetNakedEdges().Length;
            report += String.Format("Mesh has {0} naked edges. \n", nakeds);

            // Inspect mesh as solid
            if (mesh.SolidOrientation() == 1) report += "Mesh is a solid. \n";
            else if (mesh.SolidOrientation() == 0) report += "Mesh is NOT a solid. \n";
            else // inward facing normals
            {
                mesh.Flip(true, true, true);
                report += "Mesh is a solid. (normals have been flipped) \n";
            }
            
            // Attempting automatic bake
            /*GH_Mesh BakableMesh = new GH_Mesh(M);
            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            ObjectAttributes attr = 
            BakableMesh.BakeGeometry(doc, attr, ComponentGuid);
            */
            

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

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{c5e3b143-5534-4ad3-a711-33881772d683}"); }
        }
    }
}
