using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Grasshopper.Kernel.Expressions;

namespace IntraLattice
{
    public class HeterogenGradient : GH_Component
    {
        public HeterogenGradient() : base("Heterogen Gradient", "HeterogenGradient", "Heterogeneous solidification (thickness gradient) of lattice wireframe", "IntraLattice2", "Mesh") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Lines", "L", "Wireframe to thicken", GH_ParamAccess.list);
            pManager.AddTextParameter("Gradient String", "Grad", "The spatial gradient as an expression string", GH_ParamAccess.item, "1");
            pManager.AddNumberParameter("Maximum Radius", "Rmax", "Maximum radius in gradient", GH_ParamAccess.item);
            pManager.AddNumberParameter("Minimum Radius", "Rmin", "Minimum radius in gradient", GH_ParamAccess.item);
            pManager.AddNumberParameter("Node Depth", "N", "Offset depth for nodes", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Thickened wireframe", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //declare and set primary variables

            List<Curve> L = new List<Curve>();
            string gradientString = null;
            double RMax = 0;
            double RMin = 0;            
            double ND = 0;

            if (!DA.GetDataList(0, L)) { return; }
            if (!DA.GetData(1, ref gradientString)) { return; }
            if (!DA.GetData(2, ref RMax)) { return; }
            if (!DA.GetData(3, ref RMin)) { return; }
            if (!DA.GetData(4, ref ND)) { return; }

            if (L == null || L.Count == 0) { return; }
            // Should include some checks of the gradient expression here
            if (RMax <= 0) { return; }
            if (RMin <= 0) { return; }
            if (ND < 0) { return; }

            //declare node and strut lists and reference lookups

            double Sides = 6;
            bool O = false;

            List<Point3d> Nodes = new List<Point3d>();
            List<List<int>> NodeStruts = new List<List<int>>();

            List<Curve> Struts = new List<Curve>();
            List<int> StrutNodes = new List<int>();
            List<double> StrutRadii = new List<double>();

            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //set the index counter for matching start and end radii from input list
            int IdxL = 0;

            //register unique nodes and struts, with reference lookups
            //each full strut is broken into two half-struts, with the even-indexed
            //element being the start point, and the odd-indexed element being the end point

            //initialise first node
            Nodes.Add(L[0].PointAtStart);
            NodeStruts.Add(new List<int>());

            // Prepare bounding box domain for normalized gradient string
            BoundingBox fullBox = new BoundingBox();
            foreach (Curve strut in L)
            {
                var partBox = strut.GetBoundingBox(Plane.WorldXY);
                fullBox.Union(partBox);
            }
            double boxSizeX = fullBox.Max.X - fullBox.Min.X;
            double boxSizeY = fullBox.Max.Y - fullBox.Min.Y;
            double boxSizeZ = fullBox.Max.Z - fullBox.Min.Z;

            Rhino.Collections.Point3dList NodeLookup = new Rhino.Collections.Point3dList(Nodes);

            gradientString = GH_ExpressionSyntaxWriter.RewriteForEvaluator(gradientString);

            foreach (Curve StartL in L)
            {
                // Compute radii based on the gradientString expression
                var parser = new Grasshopper.Kernel.Expressions.GH_ExpressionParser();
                parser.AddVariable("x", (StartL.PointAtStart.X - fullBox.Min.X)/boxSizeX);
                parser.AddVariable("y", (StartL.PointAtStart.Y - fullBox.Min.Y)/boxSizeY);
                parser.AddVariable("z", (StartL.PointAtStart.Z - fullBox.Min.Z)/boxSizeZ);
                double StrutStartRadius = RMin + (parser.Evaluate(gradientString)._Double)*(RMax-RMin);
                parser.ClearVariables();
                parser.AddVariable("x", (StartL.PointAtEnd.X - fullBox.Min.X) / boxSizeX);
                parser.AddVariable("y", (StartL.PointAtEnd.Y - fullBox.Min.Y) / boxSizeY);
                parser.AddVariable("z", (StartL.PointAtEnd.Z - fullBox.Min.Z) / boxSizeZ);
                double StrutEndRadius = RMin + (parser.Evaluate(gradientString)._Double)*(RMax-RMin);

                Point3d StrutCenter = new Point3d((StartL.PointAtStart + StartL.PointAtEnd) / 2);

                int StartTestIdx = NodeLookup.ClosestIndex(StartL.PointAtStart);
                if (Nodes[StartTestIdx].DistanceTo(StartL.PointAtStart) < tol)
                {
                    NodeStruts[StartTestIdx].Add(Struts.Count);
                    StrutNodes.Add(StartTestIdx);
                }
                else
                {
                    StrutNodes.Add(Nodes.Count);
                    Nodes.Add(StartL.PointAtStart);
                    NodeLookup.Add(StartL.PointAtStart);
                    NodeStruts.Add(new List<int>());
                    NodeStruts.Last().Add(Struts.Count());
                }
                Struts.Add(new LineCurve(StartL.PointAtStart, StrutCenter));
                StrutRadii.Add(StrutStartRadius);


                int EndTestIdx = NodeLookup.ClosestIndex(StartL.PointAtEnd);
                if (Nodes[EndTestIdx].DistanceTo(StartL.PointAtEnd) < tol)
                {
                    NodeStruts[EndTestIdx].Add(Struts.Count);
                    StrutNodes.Add(EndTestIdx);
                }
                else
                {
                    StrutNodes.Add(Nodes.Count);
                    Nodes.Add(StartL.PointAtEnd);
                    NodeLookup.Add(StartL.PointAtEnd);
                    NodeStruts.Add(new List<int>());
                    NodeStruts.Last().Add(Struts.Count);
                }
                Struts.Add(new LineCurve(StartL.PointAtEnd, StrutCenter));
                StrutRadii.Add(StrutEndRadius);

                IdxL += 1;
            }


            Plane[] StrutPlanes = new Plane[Struts.Count]; //base plane for each strut
            double[] PlaneOffsets = new double[Struts.Count]; //distance for each base plane to be set from node
            Point3d[,] StrutHullVtc = new Point3d[Struts.Count, (int)Sides]; //two-dimensional array for vertices along each strut for executing hull
            bool[] StrutSolo = new bool[Struts.Count]; //tag for struts that don't share a node with other struts (ends)

            Mesh Hulls = new Mesh(); //main output mesh

            //cycle through each node to generate hulls
            for (int NodeIdx = 0; NodeIdx < Nodes.Count; NodeIdx++)
            {
                List<int> StrutIndices = NodeStruts[NodeIdx];
                double MinOffset = 0;
                double MaxRadius = 0;

                //orientation & size drivers for knuckle vertices
                List<Vector3d> Knuckles = new List<Vector3d>();
                double KnuckleMin = 0;

                //compare all unique combinations of struts in a given node to calculate plane offsets for
                //hulling operations and for adjusting vertices to potential non-convex hulls
                for (int I1 = 0; I1 < StrutIndices.Count - 1; I1++)
                {
                    for (int I2 = I1 + 1; I2 < StrutIndices.Count; I2++)
                    {
                        //identify minimum offset distances for calculating hulls by comparing each outgoing strut to each other
                        double R1 = StrutRadii[StrutIndices[I1]] / Math.Cos(Math.PI / Sides);
                        double R2 = StrutRadii[StrutIndices[I2]] / Math.Cos(Math.PI / Sides);
                        double Radius = Math.Max(R1, R2);
                        double Theta = Vector3d.VectorAngle(Struts[StrutIndices[I1]].TangentAtStart, Struts[StrutIndices[I2]].TangentAtStart);
                        double TestOffset = Radius * Math.Cos(Theta * 0.5) / Math.Sin(Theta * 0.5);
                        if (TestOffset > MinOffset) MinOffset = TestOffset;
                        if (MaxRadius < Radius) MaxRadius = Radius;
                        //set offsets for shrinking hull vertices into a non-convex hull based on desired node offsets (ND) and adjacent struts
                        double Offset1 = 0;
                        double Offset2 = 0;

                        //calculates final offsets for potential shrinking of nodes into non-convex hull
                        ExoTools.OffsetCalculator(Theta, R1, R2, ref Offset1, ref Offset2);

                        if (PlaneOffsets[StrutIndices[I1]] < Offset1) PlaneOffsets[StrutIndices[I1]] = Math.Max(Offset1, ND);
                        if (PlaneOffsets[StrutIndices[I2]] < Offset2) PlaneOffsets[StrutIndices[I2]] = Math.Max(Offset2, ND);

                        //set offsets for knuckle to be the size of the smallest outgoing strut radius
                        double KnuckleMinSet = Math.Min(StrutRadii[StrutIndices[I1]], StrutRadii[StrutIndices[I2]]);
                        if (I1 == 0 || KnuckleMin > KnuckleMinSet) KnuckleMin = KnuckleMinSet;
                    }
                }

                //ensures that if a two struts are linear to one another in a two-strut node that there is an offset for hulling 
                if (MinOffset < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) MinOffset = MaxRadius * 0.5;

                //direction for dealing with struts at ends
                if (StrutIndices.Count == 1)
                {
                    PlaneOffsets[StrutIndices[0]] = 0;
                    //PlaneOffsets[StrutIndices[0]] = ND;
                    if (O) StrutSolo[StrutIndices[0]] = true;
                }

                //build base planes, offset them for hulling, and array hull vertices for each strut
                for (int I1 = 0; I1 < StrutIndices.Count; I1++)
                {
                    //build base planes
                    Curve Strut = Struts[StrutIndices[I1]];
                    Plane StrutPlane;

                    //sets the strut plane
                    if (StrutIndices[I1] % 2 == 0) Strut.PerpendicularFrameAt(0, out StrutPlane);
                    else
                    {
                        Curve LookupStrut = Struts[StrutIndices[I1] - 1];
                        LookupStrut.PerpendicularFrameAt(0, out StrutPlane);
                    }

                    //offset planes for hulling
                    StrutPlane.Origin = Strut.PointAtStart + Strut.TangentAtStart * MinOffset;
                    double StrutRadius = StrutRadii[StrutIndices[I1]];

                    //add strut tangent to list of knuckle orientation vectors
                    Knuckles.Add(-Strut.TangentAtStart);

                    //add hulling vertices
                    for (int HV = 0; HV < Sides; HV++) StrutHullVtc[StrutIndices[I1], HV] = StrutPlane.PointAt(Math.Cos((HV / Sides) * Math.PI * 2) * StrutRadius, Math.Sin((HV / Sides) * Math.PI * 2) * StrutRadius);

                    double OffsetMult = PlaneOffsets[StrutIndices[I1]];
                    if (ND > OffsetMult) OffsetMult = ND;
                    if (StrutIndices[I1] % 2 != 0) StrutPlane.Rotate(Math.PI, StrutPlane.YAxis, StrutPlane.Origin);

                    if (StrutIndices.Count == 1) OffsetMult = 0;

                    StrutPlanes[StrutIndices[I1]] = StrutPlane;
                    StrutPlanes[StrutIndices[I1]].Origin = Strut.PointAtStart + Strut.TangentAtStart * OffsetMult;

                }

                //collect all of the hull points from each strut in a given node for hulling, including knuckle points

                if (StrutIndices.Count > 1)
                {
                    List<Point3d> HullPts = new List<Point3d>();
                    List<int> PlaneIndices = new List<int>();

                    double KnuckleOffset = MinOffset * 0.5;

                    for (int HV = 0; HV < Sides; HV++)
                    {
                        for (int I1 = 0; I1 < StrutIndices.Count; I1++)
                        {
                            HullPts.Add(StrutHullVtc[StrutIndices[I1], HV]);
                            PlaneIndices.Add(StrutIndices[I1]);
                            if (HV == 0)
                            {
                                HullPts.Add(Nodes[NodeIdx] + (Knuckles[I1] * KnuckleOffset));
                                PlaneIndices.Add(-1);
                            }
                        }
                        double Angle = ((double)HV / Sides) * (Math.PI * 2);
                    }

                    Rhino.Collections.Point3dList LookupPts = new Rhino.Collections.Point3dList(HullPts);

                    //execute the hulling operation
                    Mesh HullMesh = new Mesh();
                    ExoTools.Hull(HullPts, ref HullMesh);
                    ExoTools.NormaliseMesh(ref HullMesh);

                    Point3d[] HullVertices = HullMesh.Vertices.ToPoint3dArray();
                    List<int> FaceVertices = new List<int>();

                    //relocate vertices to potentially non-convex configurations
                    for (int HullVtx = 0; HullVtx < HullVertices.Length; HullVtx++)
                    {
                        int CloseIdx = LookupPts.ClosestIndex(HullVertices[HullVtx]);
                        if (PlaneIndices[CloseIdx] > -1)
                        {
                            double OffsetMult = 0;
                            if (ND > PlaneOffsets[PlaneIndices[CloseIdx]]) OffsetMult = ND - MinOffset;
                            else OffsetMult = PlaneOffsets[PlaneIndices[CloseIdx]] - MinOffset;

                            HullVertices[HullVtx] += StrutPlanes[PlaneIndices[CloseIdx]].ZAxis * OffsetMult;
                            HullMesh.Vertices[HullVtx] = new Point3f((float)HullVertices[HullVtx].X, (float)HullVertices[HullVtx].Y, (float)HullVertices[HullVtx].Z);
                            FaceVertices.Add(PlaneIndices[CloseIdx]);
                        }
                        else
                        {
                            Vector3d KnuckleVector = new Vector3d(HullVertices[HullVtx] - Nodes[NodeIdx]);
                            KnuckleVector.Unitize();
                            Point3d KnucklePt = Nodes[NodeIdx] + (KnuckleVector * KnuckleMin);
                            HullMesh.Vertices[HullVtx] = new Point3f((float)KnucklePt.X, (float)KnucklePt.Y, (float)KnucklePt.Z);
                            FaceVertices.Add(PlaneIndices[CloseIdx]);
                        }
                    }

                    //delete all faces whose vertices are associated with the same plane
                    List<int> DeleteFaces = new List<int>();

                    for (int FaceIdx = 0; FaceIdx < HullMesh.Faces.Count; FaceIdx++)
                    {
                        if ((FaceVertices[HullMesh.Faces[FaceIdx].A] != -1) && (FaceVertices[HullMesh.Faces[FaceIdx].A] == FaceVertices[HullMesh.Faces[FaceIdx].B]) &&
                            (FaceVertices[HullMesh.Faces[FaceIdx].B] == FaceVertices[HullMesh.Faces[FaceIdx].C])) DeleteFaces.Add(FaceIdx);
                    }
                    HullMesh.Faces.DeleteFaces(DeleteFaces);
                    ExoTools.NormaliseMesh(ref HullMesh);
                    Hulls.Append(HullMesh);
                }
                else if (!O)
                {
                    Mesh EndMesh = new Mesh();

                    double KnuckleOffset = ND * 0.5;
                    EndMesh.Vertices.Add(Nodes[NodeIdx] + (Knuckles[0] * KnuckleOffset));

                    for (int HullVtx = 0; HullVtx < Sides; HullVtx++)
                    {
                        EndMesh.Vertices.Add(StrutHullVtc[StrutIndices[0], HullVtx]);
                        int StartVtx = HullVtx + 1;
                        int EndVtx = HullVtx + 2;
                        if (HullVtx == Sides - 1) EndVtx = 1;
                        EndMesh.Faces.AddFace(0, StartVtx, EndVtx);
                    }
                    Hulls.Append(EndMesh);
                }
            }

            //add stocking meshes between struts
            Rhino.Collections.Point3dList MatchPts = new Rhino.Collections.Point3dList(Hulls.Vertices.ToPoint3dArray());
            Mesh StrutMeshes = new Mesh();

            //if a strut is overwhelmed by its nodes then output a sphere centered on the failing strut
            Mesh FailStruts = new Mesh();

            for (int I1 = 0; I1 <= Struts.Count; I1++)
            {
                if (I1 % 2 != 0)
                {

                    if (StrutPlanes[I1 - 1].Origin.DistanceTo(Struts[I1 - 1].PointAtStart) + StrutPlanes[I1].Origin.DistanceTo(Struts[I1].PointAtStart) > Struts[I1 - 1].GetLength() * 2)
                    {
                        Plane FailPlane = new Plane(Struts[I1 - 1].PointAtStart, Struts[I1 - 1].TangentAtStart);
                        Cylinder FailCylinder = new Cylinder(new Circle(FailPlane, (StrutRadii[I1] + StrutRadii[I1 - 1]) / 2), Struts[I1 - 1].GetLength() * 2);
                        FailStruts.Append(Mesh.CreateFromCylinder(FailCylinder, 5, 10));
                    }

                    Mesh StrutMesh = new Mesh();
                    double StrutLength = StrutPlanes[I1 - 1].Origin.DistanceTo(StrutPlanes[I1].Origin);

                    //calculate the number of segments between nodes
                    double AvgRadius = (StrutRadii[I1] + StrutRadii[I1 - 1]) / 2;
                    int Segments = (int)Math.Max((Math.Round(StrutLength * 0.5 / AvgRadius) * 2), 2);
                    double PlnZ = StrutLength / Segments;

                    bool Match = true;
                    if (O && StrutSolo[I1 - 1]) Match = false;

                    //build up vertices
                    ExoTools.VertexAdd(ref StrutMesh, StrutPlanes[I1 - 1], Sides, StrutRadii[I1 - 1], Match, MatchPts, Hulls);
                    double StrutIncrement = (StrutRadii[I1] - StrutRadii[I1 - 1]) / Segments;

                    for (int PlnIdx = 1; PlnIdx <= Segments; PlnIdx++)
                    {
                        Plane PlnSet = new Plane(StrutPlanes[I1 - 1]);
                        PlnSet.Rotate((PlnIdx % 2) * -(Math.PI / Sides), PlnSet.ZAxis);
                        PlnSet.Origin = PlnSet.PointAt(0, 0, PlnIdx * PlnZ);
                        bool Affix = false;
                        if (PlnIdx == Segments && (!StrutSolo[I1] || (StrutSolo[I1] && !O))) Affix = true;
                        ExoTools.VertexAdd(ref StrutMesh, PlnSet, Sides, StrutRadii[I1 - 1] + (StrutIncrement * PlnIdx), Affix, MatchPts, Hulls);
                    }

                    //build up faces
                    ExoTools.StockingStitch(ref StrutMesh, Segments, (int)Sides);
                    ExoTools.NormaliseMesh(ref StrutMesh);
                    Hulls.Append(StrutMesh);
                }
            }

            if (FailStruts.Vertices.Count > 0)
            {
                DA.SetData(0, FailStruts);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more struts is engulfed by its nodes");
                return;
            }

            Hulls.Faces.CullDegenerateFaces();
            Hulls.Vertices.CullUnused();

            Mesh[] OutMeshes = Hulls.SplitDisjointPieces();
            for (int SplitMesh = 0; SplitMesh < OutMeshes.Length; SplitMesh++) { ExoTools.NormaliseMesh(ref OutMeshes[SplitMesh]); }

            //ExoTools.NormaliseMesh(ref Hulls);

            DA.SetDataList(0, OutMeshes);

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
                //return Exoskeleton.Properties.Resources.exoskel;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{a5e48dd2-8467-4991-95b1-15d29524de3e}"); }
        }

    }
}


