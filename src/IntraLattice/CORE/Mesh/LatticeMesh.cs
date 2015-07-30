using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Collections;
using Rhino;
using IntraLattice.Properties;

// This component converts the wireframe lattice into a solid mesh.
// ================================================================
// Based on Exoskeleton by David Stasiuk.
// It takes as input a list of lines and two radius lists (start-end).

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class LatticeMesh : GH_Component
    {

        public LatticeMesh()
            : base("LatticeMesh", "LatticeMesh",
                "Generates solid mesh of lattice wireframe.",
                "IntraLattice2", "Mesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Struts", "Struts", "Curve network", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius (start)", "Rs", "List of radii for start of struts", GH_ParamAccess.list, 0.6);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddPointParameter("Vertices", "V", "Lattice Mesh Vertices", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Lattice Mesh", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Declare placeholder variables
            List<Curve> inputStruts = new List<Curve>();
            List<double> radiusList = new List<double>();

            // Attempt to fetch data inputs
            if (!DA.GetDataList(0, inputStruts)) { return; }
            if (!DA.GetDataList(1, radiusList)) { return; }

            // Validate data
            if (inputStruts == null || inputStruts.Count == 0) { return; }
            if (radiusList == null || radiusList.Count == 0 || radiusList.Contains(0)) { return; }

            // Set some variables
            int sides = 6;  // Number of sides on each strut
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            bool latticeIsLinear = true;    // will become false if a non-linear curve is found in the strut list

            // Initialize lattice object
            Lattice lattice = new Lattice();
            // Initialize output mesh
            Mesh outMesh = new Mesh();

            //====================================================================================
            // STEP 1 - Data structure
            // In this section, we construct the wireframe lattice
            // Ensuring that no duplicate nodes or struts are present
            //====================================================================================

            // These two lookup lists are used to determine if the nodes or pairs of nodes are already defined
            // Such that we avoid duplicates
            Point3dList nodeLookup = new Point3dList();
            List<IndexPair> nodePairLookup = new List<IndexPair>();

            // Loop over list of struts
            for (int i = 0; i < inputStruts.Count; i++ )
            {
                Curve strut = inputStruts[i];

                Point3d[] nodes = new Point3d[2] { strut.PointAtStart, strut.PointAtEnd };
                List<int> nodeIndices = new List<int>();

                // Loop over end points of strut
                for (int j = 0; j < 2; j++ )
                {
                    Point3d pt = nodes[j];
                    int closestIndex = nodeLookup.ClosestIndex(pt);  // find closest node to current pt

                    // If node already exists, set the index
                    if (nodeLookup.Count != 0 && nodeLookup[closestIndex].DistanceTo(pt) < tol)
                        nodeIndices.Add(closestIndex);
                    // If node doesn't exist
                    else
                    {
                        // construct node
                        nodeLookup.Add(pt);
                        lattice.Nodes.Add(new Node(pt));
                        nodeIndices.Add(nodeLookup.Count - 1);
                    }
                }

                // If strut doesn't exist, we create it
                IndexPair nodePair = new IndexPair(nodeIndices[0], nodeIndices[1]);
                if (nodePairLookup.Count == 0 || !nodePairLookup.Contains(nodePair))
                {
                    // construct strut
                    lattice.Struts.Add(new Strut(strut, nodePair));
                    int strutIndex = lattice.Struts.Count - 1;
                    // construct plates
                    lattice.Plates.Add(new Plate(nodeIndices[0]));
                    lattice.Plates.Add(new Plate(nodeIndices[1]));
                    // set strut relational parameters
                    IndexPair platePair = new IndexPair(lattice.Plates.Count - 2, lattice.Plates.Count - 1);
                    lattice.Struts[strutIndex].PlatePair = platePair;
                    // set node relational parameters
                    lattice.Nodes[nodeIndices[0]].StrutIndices.Add(strutIndex);
                    lattice.Nodes[nodeIndices[1]].StrutIndices.Add(strutIndex);
                    lattice.Nodes[nodeIndices[0]].PlateIndices.Add(platePair.I);
                    lattice.Nodes[nodeIndices[1]].PlateIndices.Add(platePair.J);
                }

                // if lattice is thought to be linear, but the curve is not linear
                if (latticeIsLinear && !strut.IsLinear()) latticeIsLinear = false;
            }


            //====================================================================================
            // STEP 2 - Compute nodal radii
            // Strut radius is node-based
            //====================================================================================

            // Loop over nodes
            foreach (Node node in lattice.Nodes)
            {
                node.Radius = 1;
            }


            //====================================================================================
            // STEP 3 - Compute plate offsets
            // Strut radius is node-based
            //====================================================================================

            // Loop over nodes
            for (int i = 0; i < lattice.Nodes.Count; i++)
            {
                Node node = lattice.Nodes[i];

                // Loop over plates of node
                for (int j = 0; j < node.PlateIndices.Count; j++)
                {
                    int plateIndex = node.PlateIndices[j];

                    // Set offset
                    double offset = 1.2;
                    lattice.Plates[plateIndex].Offset = offset;

                }
            }

            //====================================================================================
            // STEP 4 - Construct sleeve meshes
            // 
            //====================================================================================

            // Loop over struts
            for (int i = 0; i < lattice.Struts.Count; i++)
            {
                Mesh sleeveMesh = new Mesh();

                Strut strut = lattice.Struts[i];
                Plate startPlate = lattice.Plates[strut.PlatePair.I];
                Plate endPlate = lattice.Plates[strut.PlatePair.J];
                startPlate.Vtc.Add(strut.Curve.PointAtLength(startPlate.Offset));   // centerpoint
                endPlate.Vtc.Add(strut.Curve.PointAtLength(strut.Curve.GetLength() - endPlate.Offset));
                double startRadius = lattice.Nodes[strut.NodePair.I].Radius;
                double endRadius = lattice.Nodes[strut.NodePair.J].Radius;

                // compute the number of divisions
                double avgRadius = (startRadius + endRadius) / 2;
                double length = startPlate.Vtc[0].DistanceTo(endPlate.Vtc[0]);
                double divisions = Math.Max((Math.Round(length * 0.5 / avgRadius) * 2), 2); // Number of sleeve divisions (must be even)

                // SLEEVE VERTICES
                // ================
                // if linear lattice
                if (latticeIsLinear)
                {
                    Vector3d normal = strut.Curve.TangentAtStart;

                    // Loops: j along strut, k around strut
                    for (int j = 0; j <= divisions; j++)
                    {              
                        Point3d knucklePt = startPlate.Vtc[0] + (normal * (length * j / divisions));
                        Plane plane = new Plane(knucklePt, normal);
                        double R = startRadius - j / (double)divisions * (startRadius - endRadius); //variable radius

                        for (int k = 0; k < sides; k++)
                        {
                            double angle = k * 2 * Math.PI / sides + j * Math.PI / sides;
                            sleeveMesh.Vertices.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle))); // create vertex

                            // if plate points, save them
                            if (j == 0) startPlate.Vtc.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle)));
                            if (j == divisions) endPlate.Vtc.Add(plane.PointAt(R * Math.Cos(angle), R * Math.Sin(angle)));
                        }
                    }
                }
                else
                {
                    // sleeves for curved struts here
                }

                // Create sleeve faces
                MeshTools.SleeveStitch(ref sleeveMesh, divisions, sides);
                outMesh.Append(sleeveMesh);

            }


            DA.SetData(0, outMesh);


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
                // You can add image files to your project resources and access them like this:
                //return Resources.PresetCell;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{dee24b08-fcb2-46f9-b772-9bece0903d9a}"); }
        }
    }
}
