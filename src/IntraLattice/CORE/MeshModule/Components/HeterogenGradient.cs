using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Grasshopper.Kernel.Expressions;
using Rhino.Collections;
using IntraLattice.CORE.MeshModule.Data;
using IntraLattice.CORE.FrameModule;

namespace IntraLattice.CORE.MeshModule
{
    public class HeterogenGradient : GH_Component
    {
        public HeterogenGradient()
            : base("Heterogen Gradient", "HeterogenGradient",
                "Heterogeneous solidification (thickness gradient) of lattice wireframe",
                "IntraLattice2", "Mesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Lines", "L", "Wireframe to thicken", GH_ParamAccess.list);
            pManager.AddTextParameter("Gradient String", "Grad", "The spatial gradient as an expression string", GH_ParamAccess.item, "1");
            pManager.AddNumberParameter("Maximum Radius", "Rmax", "Maximum radius in gradient", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Minimum Radius", "Rmin", "Minimum radius in gradient", GH_ParamAccess.item, 0.2);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Thickened wireframe", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 0. Declare placeholder variables
            List<Curve> strutList = new List<Curve>();
            string gradientString = null;
            double maxRadius = 0;
            double minRadius = 0;

            // 1. Attempt to fetch data inputs
            if (!DA.GetDataList(0, strutList)) { return; }
            if (!DA.GetData(1, ref gradientString)) { return; }
            if (!DA.GetData(2, ref maxRadius)) { return; }
            if (!DA.GetData(3, ref minRadius)) { return; }

            // 2. Validate data
            if (strutList == null || strutList.Count == 0) { return; }
            if (maxRadius <= 0 || minRadius <= 0) { return; }

            // 3. Set some variables
            int sides = 6;  // Number of sides on each strut
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // 4. Initialize lattice object
            LatticeMesh lattice = new LatticeMesh();

            //====================================================================================
            //  PART A - Network cleanse
            //  Clean the network of curves
            //  - Remove duplicate nodes and struts
            //  - Remove null, invalid or tiny curves
            //  - (Future idea: Combining colinear struts)
            //====================================================================================

            // A0. We use the following three lists to extract valid data from the input list
            var nodeList = new Point3dList();               // List of unique nodes
            var nodePairList = new List<IndexPair>();       // List of struts, as node index pairs
            
            strutList = FrameTools.CleanNetwork(strutList, out nodeList, out nodePairList);

            //====================================================================================
            // PART B - Data structure
            // In this section, we construct the wireframe lattice
            // Ensuring that no duplicate nodes or struts are present
            //====================================================================================

            // B0. Create nodes
            foreach (Point3d node in nodeList)
                lattice.Nodes.Add(new Node(node));

            // B1. Create struts and plates
            for (int i = 0; i < strutList.Count; i++)
            {
                lattice.Struts.Add(new Strut(strutList[i], nodePairList[i])); // assign
                // construct plates
                lattice.Plates.Add(new Plate(nodePairList[i].I, strutList[i].TangentAtStart));
                lattice.Plates.Add(new Plate(nodePairList[i].J, -strutList[i].TangentAtEnd));
                // set strut relational parameters
                IndexPair platePair = new IndexPair(lattice.Plates.Count - 2, lattice.Plates.Count - 1);
                lattice.Struts[i].PlatePair = platePair;
                // set node relational parameters
                lattice.Nodes[nodePairList[i].I].StrutIndices.Add(i);
                lattice.Nodes[nodePairList[i].J].StrutIndices.Add(i);
                lattice.Nodes[nodePairList[i].I].PlateIndices.Add(platePair.I);
                lattice.Nodes[nodePairList[i].J].PlateIndices.Add(platePair.J);
            }


            //====================================================================================
            // PART C - Compute nodal radii
            // Strut radius is node-based
            //====================================================================================

            // C0. Prepare bounding box domain for normalized gradient string
            BoundingBox fullBox = new BoundingBox();
            foreach (Strut strut in lattice.Struts)
            {
                var strutBox = strut.Curve.GetBoundingBox(Plane.WorldXY);
                fullBox.Union(strutBox);
            }
            double boxSizeX = fullBox.Max.X - fullBox.Min.X;
            double boxSizeY = fullBox.Max.Y - fullBox.Min.Y;
            double boxSizeZ = fullBox.Max.Z - fullBox.Min.Z;

            gradientString = GH_ExpressionSyntaxWriter.RewriteForEvaluator(gradientString);

            // C1. Set radii
            foreach (Node node in lattice.Nodes)
            {
                var parser = new Grasshopper.Kernel.Expressions.GH_ExpressionParser();
                parser.AddVariable("x", (node.Point3d.X - fullBox.Min.X) / boxSizeX);
                parser.AddVariable("y", (node.Point3d.Y - fullBox.Min.Y) / boxSizeY);
                parser.AddVariable("z", (node.Point3d.Z - fullBox.Min.Z) / boxSizeZ);
                node.Radius = minRadius + (parser.Evaluate(gradientString)._Double) * (maxRadius - minRadius);
                parser.ClearVariables();
            }


            //====================================================================================
            // PART D - Compute plate offsets
            // Each plate is offset from its parent node, to avoid mesh overlaps.
            //====================================================================================

            // D0. Loop over nodes
            for (int i = 0; i < lattice.Nodes.Count; i++)
            {
                // if node has only 1 strut, skip it
                if (lattice.Nodes[i].StrutIndices.Count < 2) continue;
                // compute the offsets required to avoid plate overlaps
                lattice.ComputeOffsets(i, tol);
                // To improve convex hull shape at 'sharp' nodes, we add an extra plate
                lattice.FixSharpNodes(i, sides);
            }

            // IDEA : add a new loop here that adjusts radii to avoid overlapping struts

            //====================================================================================
            // PART E - Construct sleeve meshes and hull points
            // 
            //====================================================================================

            // E0. Loop over struts
            for (int i = 0; i < lattice.Struts.Count; i++)
            {
                Mesh sleeveMesh = lattice.MakeSleeve(i, sides);
                // append the new sleeve mesh to the full lattice mesh
                lattice.Mesh.Append(sleeveMesh);
            }

            //====================================================================================
            // STEP 5 - Construct hull meshes
            // 
            //====================================================================================

            // HULLS - Loop over all nodes
            for (int i = 0; i < lattice.Nodes.Count; i++)
            {
                Node node = lattice.Nodes[i];

                int plateCount = lattice.Nodes[i].PlateIndices.Count;
                // If node has a single plate, create an endmesh
                if (plateCount < 2)
                {
                    Mesh endMesh = lattice.MakeEndFace(i, sides);
                    lattice.Mesh.Append(endMesh);
                }
                // If node has more than 1 plate, create a hullmesh
                else
                {
                    Mesh hullMesh = lattice.MakeConvexHull(i, sides, tol, true);
                    lattice.Mesh.Append(hullMesh);
                }
            }

            // POST-PROCESS FINAL MESH
            lattice.Mesh.Vertices.CombineIdentical(true, true);
            lattice.Mesh.FaceNormals.ComputeFaceNormals();
            lattice.Mesh.UnifyNormals();
            lattice.Mesh.Normals.ComputeNormals();


            DA.SetData(0, lattice.Mesh);
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
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{a5e48dd2-8467-4991-95b1-15d29524de3e}"); }
        }

    }
}


