using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.Collections;
using IntraLattice.CORE.Data;
using IntraLattice.CORE.Components;
using IntraLattice.CORE.Helpers;

// Summary:     This component generates a solid mesh of a curve network, with constant strut radii.
//              General approach based on Exoskeleton by David Stasiuk.
// ===============================================================================
// Details:     - Lacks robustness: if strut radius is too thick, and results in overlaps, meshing fails.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.MeshModule
{
    public class HomogenComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the HomogenComponent class.
        /// </summary>
        public HomogenComponent()
            : base("Homogen","Homogen",
                "Homogeneous solidification of lattice wireframe",
                "IntraLattice2", "Mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Wireframe to thicken", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius", "Radius", "Strut Radius", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Thickened wireframe", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables
            List<Curve> struts = new List<Curve>();
            double radius = 0;

            // 2. Attempt to fetch data inputs
            if (!DA.GetDataList(0, struts)) { return; }
            if (!DA.GetData(1, ref radius)) { return; }

            // 3. Validate data
            if (struts == null || struts.Count == 0) { return; }
            if (radius <= 0) { return; }

            // 4. Set some variables
            int sides = 6;  // Number of sides on each strut
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // 5. Initialize lattice object
            // This constructor cleans the curve network (removes duplicates), and formats it as an ExoMesh.
            ExoMesh exoMesh = new ExoMesh(struts);

            //====================================================================================
            // PART A - Compute radii
            // Set the start/end radii of each sleeve, based on user input.
            //====================================================================================

            // C0. Set radii
            foreach (ExoSleeve sleeve in exoMesh.Sleeves)
            {
                sleeve.StartRadius = radius;
                sleeve.EndRadius = radius;
            }

            //====================================================================================
            // PART B - Compute plate offsets
            // Each plate is offset from its parent node, to avoid mesh overlaps.
            // We also ensure that the no plates are engulfed by the hulls, so we're looking for
            // a convex plate layout. If any plate vertex gets engulfed, meshing will fail.
            //====================================================================================

            // B0. Loop over nodes
            for (int i = 0; i < exoMesh.Hulls.Count; i++)
            {
                // If node has only 1 strut, skip it
                if (exoMesh.Hulls[i].SleeveIndices.Count < 2)
                {
                    continue;
                }
                // Compute the offsets required to avoid plate overlaps
                bool success = exoMesh.ComputeOffsets(i, tol);
                // To improve convex hull shape at 'sharp' nodes, we add an extra plate
                exoMesh.FixSharpNodes(i, sides);
            }

            // IDEA : add a new loop here that adjusts radii to avoid overlapping struts


            //====================================================================================
            // PART C - Construct sleeve meshes and hull points
            // 
            //====================================================================================

            // E0. Loop over all sleeves
            for (int i = 0; i < exoMesh.Sleeves.Count; i++)
            {
                Mesh sleeveMesh = exoMesh.MakeSleeve(i, sides);
                // append the new sleeve mesh to the full lattice mesh
                exoMesh.Mesh.Append(sleeveMesh);
            }

            //====================================================================================
            // PART D - Construct hull meshes
            // Generates convex hulls, then removes the faces that lie on the plates.
            //====================================================================================

            var hullMeshList = new List<Mesh>();

            // D0. Loop over all hulls
            for (int i = 0; i < exoMesh.Hulls.Count; i++)
            {
                ExoHull node = exoMesh.Hulls[i];

                int plateCount = exoMesh.Hulls[i].PlateIndices.Count;
                // If node has a single plate, create an endmesh
                if (plateCount < 2)
                {
                    Mesh endMesh = exoMesh.MakeEndFace(i, sides);
                    exoMesh.Mesh.Append(endMesh);
                }
                // If node has more than 1 plate, create a hullmesh
                else
                {
                    Mesh hullMesh = exoMesh.MakeConvexHull(i, sides, tol, true);
                    hullMeshList.Add(hullMesh);
                    exoMesh.Mesh.Append(hullMesh);
                }
            }

            List<Circle> plateCircles = new List<Circle>();

            foreach (ExoPlate plate in exoMesh.Plates)
            {
                plateCircles.Add(new Circle(plate.Vtc[1], plate.Vtc[2], plate.Vtc[3]));
            }

            // 6. Post-process the final mesh.
            exoMesh.Mesh.Vertices.CombineIdentical(true, true);
            exoMesh.Mesh.FaceNormals.ComputeFaceNormals();
            exoMesh.Mesh.UnifyNormals();
            exoMesh.Mesh.Normals.ComputeNormals();

            // 7. Set output
            DA.SetData(0, exoMesh.Mesh);
        }

        /// <summary>
        /// Sets the exposure of the component (i.e. the toolbar panel it is in)
        /// </summary>
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{a51ac688-3afc-48a5-b121-48cecf687eb5}"); }
        }
        
    }
}




