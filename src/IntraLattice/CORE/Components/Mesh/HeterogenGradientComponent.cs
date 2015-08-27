using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Grasshopper.Kernel.Expressions;
using Rhino.Collections;
using IntraLattice.CORE.Data;
using IntraLattice.CORE.Components;
using IntraLattice.CORE.Helpers;

// Summary:     This component generates a solid mesh of a curve network, with gradient strut radii.
//              Inspired by Exoskeleton (by David Stasiuk).
// ===============================================================================
// Details:     - Strut radii based on user-input mathematical expression f(x,y,z), representing a spatial gradient.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.MeshModule
{
    public class HeterogenGradientComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the HeterogenGradientComponent class.
        /// </summary>
        public HeterogenGradientComponent()
            : base("Heterogen Gradient", "HeterogenGradient",
                "Heterogeneous solidification (thickness gradient) of lattice wireframe",
                "IntraLattice2", "Mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Wireframe to thicken", GH_ParamAccess.list);
            pManager.AddTextParameter("Gradient String", "Grad", "The spatial gradient as an expression string", GH_ParamAccess.item, "0");
            pManager.AddNumberParameter("Maximum Radius", "Rmax", "Maximum radius in gradient", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Minimum Radius", "Rmin", "Minimum radius in gradient", GH_ParamAccess.item, 0.2);
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
            string gradientString = null;
            double maxRadius = 0;
            double minRadius = 0;

            // 2. Attempt to fetch data inputs
            if (!DA.GetDataList(0, struts)) { return; }
            if (!DA.GetData(1, ref gradientString)) { return; }
            if (!DA.GetData(2, ref maxRadius)) { return; }
            if (!DA.GetData(3, ref minRadius)) { return; }

            // 3. Validate data
            if (struts == null || struts.Count == 0) { return; }
            if (maxRadius <= 0 || minRadius <= 0) { return; }

            // 4. Set some variables
            int sides = 6;  // Number of sides on each strut
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // 5. Initialize lattice object
            // This constructor cleans the curve network (removes duplicates), and formats it as an ExoMesh.
            ExoMesh exoMesh = new ExoMesh(struts);

            //====================================================================================
            // PART A - Compute radii
            // Set the start/end radii of each sleeve, based on spatial gradient.
            //====================================================================================

            // A0. Prepare bounding box domain for normalized gradient string
            BoundingBox fullBox = new BoundingBox();
            foreach (ExoSleeve sleeve in exoMesh.Sleeves)
            {
                var strutBox = sleeve.Curve.GetBoundingBox(Plane.WorldXY);
                fullBox.Union(strutBox);
            }
            double boxSizeX = fullBox.Max.X - fullBox.Min.X;
            double boxSizeY = fullBox.Max.Y - fullBox.Min.Y;
            double boxSizeZ = fullBox.Max.Z - fullBox.Min.Z;

            gradientString = GH_ExpressionSyntaxWriter.RewriteForEvaluator(gradientString);

            // A1. Set radii
            foreach (ExoSleeve sleeve in exoMesh.Sleeves)
            {
                // Start node
                ExoHull node = exoMesh.Hulls[sleeve.HullPair.I];
                var parser = new Grasshopper.Kernel.Expressions.GH_ExpressionParser();
                parser.AddVariable("x", (node.Point3d.X - fullBox.Min.X) / boxSizeX);
                parser.AddVariable("y", (node.Point3d.Y - fullBox.Min.Y) / boxSizeY);
                parser.AddVariable("z", (node.Point3d.Z - fullBox.Min.Z) / boxSizeZ);
                sleeve.StartRadius = minRadius + (parser.Evaluate(gradientString)._Double) * (maxRadius - minRadius);
                parser.ClearVariables();
                // End node
                node = exoMesh.Hulls[sleeve.HullPair.J];
                parser.AddVariable("x", (node.Point3d.X - fullBox.Min.X) / boxSizeX);
                parser.AddVariable("y", (node.Point3d.Y - fullBox.Min.Y) / boxSizeY);
                parser.AddVariable("z", (node.Point3d.Z - fullBox.Min.Z) / boxSizeZ);
                sleeve.EndRadius = minRadius + (parser.Evaluate(gradientString)._Double) * (maxRadius - minRadius);
                parser.ClearVariables();
            }

            //====================================================================================
            // PART B - Compute plate offsets
            // Each plate is offset from its parent node, to avoid mesh overlaps.
            // We also need to ensure that no plates are engulfed by the hulls, so we're 
            // looking for a convex plate layout. If any plate vertex gets engulfed, meshing will fail.
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

            // C0. Loop over all sleeves
            for (int i = 0; i < exoMesh.Sleeves.Count; i++)
            {
                Mesh sleeveMesh = exoMesh.MakeSleeve(i, sides);
                // Append the new sleeve mesh to the full lattice mesh
                exoMesh.Mesh.Append(sleeveMesh);
            }

            //====================================================================================
            // PART D - Construct hull meshes
            // Generates convex hulls, then removes the faces that lie on the plates.
            //====================================================================================

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
                    exoMesh.Mesh.Append(hullMesh);
                }
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
                return GH_Exposure.secondary;
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
            get { return new Guid("{a5e48dd2-8467-4991-95b1-15d29524de3e}"); }
        }

    }
}


