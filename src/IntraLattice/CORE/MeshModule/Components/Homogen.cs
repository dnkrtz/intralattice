using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.Collections;
using IntraLattice.CORE.MeshModule.Data;

namespace IntraLattice.CORE.MeshModule
{
    public class Homogen : GH_Component
    {
        public Homogen()
            : base("Homogen","Homogen",
                "Homogeneous solidification of lattice wireframe",
                "IntraLattice2", "Mesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Wireframe to thicken", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius", "Radius", "Strut Radius", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Thickened wireframe", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 0. Declare placeholder variables
            List<Curve> inputStruts = new List<Curve>();
            double radius = 0;

            // 1. Attempt to fetch data inputs
            if (!DA.GetDataList(0, inputStruts)) { return; }
            if (!DA.GetData(1, ref radius)) { return; }

            // 2. Validate data
            if (inputStruts == null || inputStruts.Count == 0) { return; }
            if (radius <= 0) { return; }

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
            var strutList = new List<Curve>();              // List of struts, as curves (parallel to nodePairList)

            MeshTools.CleanNetwork(inputStruts, out nodeList, out nodePairList, out strutList);

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

            // C0. Set radii
            foreach (Node node in lattice.Nodes)
            {
                node.Radius = radius;
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
                return GH_Exposure.primary;
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
            get { return new Guid("{a51ac688-3afc-48a5-b121-48cecf687eb5}"); }
        }
        
    }
}




