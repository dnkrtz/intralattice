using Grasshopper.Kernel.Data;
using IntraLattice.CORE.Helpers;
using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Summary:     This set of classes is used to generate a solid mesh of a lattice wireframe.
//              Refer to the developer documentation for more information.
// =====================================================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Data
{

    class ExoMesh
    {
        #region Fields
        private List<ExoHull> m_hulls;
        private List<ExoSleeve> m_sleeves;
        private List<ExoPlate> m_plates;
        private Mesh m_mesh;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ExoMesh()
        {
            m_hulls = new List<ExoHull>();
            m_sleeves = new List<ExoSleeve>();
            m_plates = new List<ExoPlate>();
            m_mesh = new Mesh();
        }
        /// <summary>
        /// Instace constructor based on a list of curves (i.e. a lattice).
        /// </summary>
        public ExoMesh(List<Curve> struts)
        {
            m_hulls = new List<ExoHull>();
            m_sleeves = new List<ExoSleeve>();
            m_plates = new List<ExoPlate>();
            m_mesh = new Mesh();

            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // First, we convert the struts to a list of unique nodes and node pairs
            // We use the following lists to extract valid data from the input list
            var nodeList = new Point3dList();               // List of unique nodes
            var nodePairList = new List<IndexPair>();       // List of struts, as node index pairs
            struts = FrameTools.CleanNetwork(struts, tol, out nodeList, out nodePairList);

            // Set hull locations
            foreach (Point3d node in nodeList)
                m_hulls.Add(new ExoHull(node));

            // Create sleeves, plates and relational indices
            for (int i = 0; i < struts.Count; i++)
            {
                m_sleeves.Add(new ExoSleeve(struts[i], nodePairList[i]));
                // Construct plates
                m_plates.Add(new ExoPlate(nodePairList[i].I, struts[i].TangentAtStart));
                m_plates.Add(new ExoPlate(nodePairList[i].J, -struts[i].TangentAtEnd));
                // Set sleeve relational parameters
                IndexPair platePair = new IndexPair(m_plates.Count - 2, m_plates.Count - 1);
                m_sleeves[i].PlatePair = platePair;
                // Set hull relational parameters
                m_hulls[nodePairList[i].I].SleeveIndices.Add(i);
                m_hulls[nodePairList[i].J].SleeveIndices.Add(i);
                m_hulls[nodePairList[i].I].PlateIndices.Add(platePair.I);
                m_hulls[nodePairList[i].J].PlateIndices.Add(platePair.J);
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// List of nodes in the lattice (as ExoHull objects).
        /// </summary>
        public List<ExoHull> Hulls
        {
            get { return m_hulls; }
            set { m_hulls = value; }
        }
        /// <summary>
        /// List of struts in the lattice (as ExoSleeve objects).
        /// </summary>
        public List<ExoSleeve> Sleeves
        {
            get { return m_sleeves; }
            set { m_sleeves = value; }
        }
        /// <summary>
        /// List of plates in the lattice (as Plate objects). Plates are essentially the vertices that are shared between sleeve and hull meshes.
        /// </summary>
        public List<ExoPlate> Plates
        {
            get { return m_plates; }
            set { m_plates = value; }
        }
        /// <summary>
        /// The actual mesh.
        /// </summary>
        public Mesh Mesh
        {
            get { return m_mesh; }
            set { m_mesh = value; }
        }
        #endregion

        #region Pre-Processing Methods
        /// <summary>
        /// Computes plate offsets required to avoid mesh overlaps.
        /// A robust, incremental approach is used.
        /// </summary>
        /// <param name="nodeIndex">Index of the node who's plates we are computing offsets for.</param>
        /// <param name="tol">Tolerance for point locations (RhinoDoc.ActiveDoc.ModelAbsoluteTolerance is a good bet).</param>
        /// <returns>True if offsets are valid, false if struts are engulfed by their nodes.</returns>
        public bool ComputeOffsets(int nodeIndex, double tol)
        {
            ExoHull node = Hulls[nodeIndex];

            List<Curve> paths = new List<Curve>();
            List<double> radii = new List<double>();
            List<double> offsets = new List<double>();  // parameter offset (path domains are unitized)

            // Prepare all struts and initialize offsets
            foreach (int strutIndex in node.SleeveIndices)
            {
                Curve curve = Sleeves[strutIndex].Curve.DuplicateCurve();
                // If curve doesn't start at this node, reverse the curve and save end radius
                if (curve.PointAtEnd.EpsilonEquals(node.Point3d, 100 * tol))
                {
                    curve.Reverse(); // reverse direction of curve to start at this node
                    curve.Domain = new Interval(0, 1);
                    radii.Add(Sleeves[strutIndex].EndRadius);
                }
                else
                {
                    radii.Add(Sleeves[strutIndex].StartRadius);
                }

                paths.Add(curve);
                
                // We start at an offset equal to the strut radius at the node (this is our minimum offset).
                // Get the starting parameter at this offset
                double offsetParam;
                curve.LengthParameter(radii[radii.Count-1], out offsetParam);
                offsets.Add(offsetParam);
            }

            // Compute avg radius at the node (mainly used for sharp node extra plate)
            double sumRadii = 0;
            foreach (double radius in radii)
                sumRadii += radius;
            node.AvgRadius = sumRadii / radii.Count;

            bool convexFound = false;
            bool[] travel;
            int iteration = 0;
            double paramIncrement = offsets[0] / 10;
            // Iterate until a suitable plate layout is found:
            // - Sleeves won't overlap
            // - Hulls won't engulf any of the plate points (all points must lie ON the convex hull)
            while (!convexFound && iteration < 500)
            {
                // Prepare list of circles
                List<Circle> circles = new List<Circle>();
                for (int i = 0; i < paths.Count; i++)
                {
                    Plane plane;
                    paths[i].PerpendicularFrameAt(offsets[i], out plane);
                    circles.Add(new Circle(plane, radii[i]));
                }

                // Loop over all pairs of struts
                travel = new bool[paths.Count];
                for (int a = 0; a < paths.Count; a++)
                {
                    for (int b = a + 1; b < paths.Count; b++)
                    {
                        double p1, p2;
                        var intAB = Intersection.PlaneCircle(circles[a].Plane, circles[b], out p1, out p2);
                        var intBA = Intersection.PlaneCircle(circles[b].Plane, circles[a], out p1, out p2);
                        if (intAB == PlaneCircleIntersection.Secant || intAB == PlaneCircleIntersection.Tangent)
                            travel[a] = true;
                        if (intBA == PlaneCircleIntersection.Secant || intBA == PlaneCircleIntersection.Tangent)
                            travel[b] = true;
                    }
                }

                // Increase offset of plates that intersected, if no intersections, we have a suitable convex layout
                convexFound = true;
                for (int i = 0; i < paths.Count; i++)
                {
                    if (travel[i])
                    {
                        offsets[i] += paramIncrement;
                        convexFound = false;
                    }
                }

                iteration++;
            }

            for (int i = 0; i < paths.Count; i++)
            {
                int plateIndex = node.PlateIndices[i];
                this.Plates[plateIndex].Offset = 1.05 * offsets[i];
            }

            return true;
        } 
        /// <summary>
        /// Adds a plate to the node if it is a 'sharp' node, to improve convex hull shape.
        /// </summary>
        /// <param name="nodeIndex"> Index of the node we want to check/fix. </param>
        /// <param name="sides"> Number of sides on the sleeve meshes. </param>
        public void FixSharpNodes(int nodeIndex, int sides)
        {
            ExoHull node = Hulls[nodeIndex];

            // The extra plate is in the direction of the negative sum of all normals
            // We use the new plate normal to check if the node struts are contained within a 180deg peripheral (i.e. the node is 'sharp')
            bool isSharp = true;
            Vector3d extraNormal = new Vector3d();  // sum of all normals
            foreach (int plateIndex in node.PlateIndices)
                extraNormal += this.Plates[plateIndex].Normal;
            foreach (int plateIndex in node.PlateIndices)
                if (Vector3d.VectorAngle(-extraNormal, this.Plates[plateIndex].Normal) < Math.PI / 2)
                    isSharp = false;

            //  If struts form a sharp corner, add an extra plate for a better convex hull shape
            if (isSharp)
            {

                // Plane offset from node slightly
                Plane plane = new Plane(node.Point3d - extraNormal * node.AvgRadius / 3, -extraNormal);
                List<Point3d> Vtc = MeshTools.CreateKnuckle(plane, sides, node.AvgRadius, 0);    // compute the vertices
                // Add new plate and its vertices
                this.Plates.Add(new ExoPlate(nodeIndex, -extraNormal));
                int newPlateIndx = this.Plates.Count - 1;
                this.Plates[newPlateIndx].Vtc.AddRange(Vtc);
                node.PlateIndices.Add(newPlateIndx);
            }
        }
        #endregion

        #region Meshing Methods
        /// <summary>
        /// Generates sleeve mesh for the struts. The plate offsets should be set before you use this method.
        /// </summary>
        /// <param name="strutIndex">Index of the strut being thickened.</param>
        /// <param name="sides">Number of sides for the strut mesh.</param>
        /// <param name="sleeveMesh">The sleeve mesh</param>
        public Mesh MakeSleeve(int strutIndex, int sides)
        {
            Mesh sleeveMesh = new Mesh();
            ExoSleeve strut = this.Sleeves[strutIndex];
            ExoPlate startPlate = this.Plates[strut.PlatePair.I];   // plate for the start of the strut
            ExoPlate endPlate = this.Plates[strut.PlatePair.J];
            double startParam, endParam;
            startParam = startPlate.Offset;
            endParam = 1 - endPlate.Offset;

            // Set center point of start & end plates
            startPlate.Vtc.Add(strut.Curve.PointAt(startParam));
            endPlate.Vtc.Add(strut.Curve.PointAt(endParam));

            // Compute the number of sleeve divisions (must be even)
            double avgRadius = strut.AvgRadius;
            double length = strut.Curve.GetLength(new Interval(startParam, endParam));
            double divisions = Math.Max((Math.Round(length * 0.5 / avgRadius) * 2), 2); 

            // Generate sleeve vertices
            Vector3d normal = strut.Curve.TangentAtStart;
            // Loops along strut
            for (int j = 0; j <= divisions; j++)
            {
                Plane plane;
                // For linear struts
                if (strut.Curve.IsLinear())
                {
                    Point3d knucklePt = startPlate.Vtc[0] + (normal * (length * j / divisions));
                    plane = new Plane(knucklePt, normal);
                }
                // For curved struts, we compute a new perpendicular frame at every iteration
                else 
                {
                    double locParameter = startParam + (j / divisions) * (endParam - startParam);
                    Point3d knucklePt = strut.Curve.PointAt(locParameter);
                    strut.Curve.PerpendicularFrameAt(locParameter, out plane);
                }
                double R = strut.StartRadius - j * (strut.StartRadius - strut.EndRadius)/ (double)divisions; //variable radius
                double startAngle = j * Math.PI / sides; // this twists the plate points along the strut, for triangulation

                List<Point3d> Vtc = MeshTools.CreateKnuckle(plane, sides, R, startAngle);    // compute the vertices

                // If the vertices are hull points (plates that connect sleeves to node hulls), save them
                if (j == 0) startPlate.Vtc.AddRange(Vtc);
                if (j == divisions) endPlate.Vtc.AddRange(Vtc);

                sleeveMesh.Vertices.AddVertices(Vtc); // save vertices to sleeve mesh
            }

            // Generate sleeve mesh (stitch vertices)
            int V1, V2, V3, V4;
            for (int j = 0; j < divisions; j++)
            {
                for (int i = 0; i < sides; i++)
                {
                    V1 = (j * sides) + i;
                    V2 = (j * sides) + i + sides;
                    V3 = (j * sides) + sides + (i + 1) % (sides);
                    V4 = (j * sides) + (i + 1) % (sides);

                    sleeveMesh.Faces.AddFace(V1, V2, V4);
                    sleeveMesh.Faces.AddFace(V2, V3, V4);
                }
            }

            return sleeveMesh;
        }
        /// <summary>
        /// Generates a convex hull mesh for a set of points. Also removes all faces that lie on the ExoMesh plates.
        /// </summary>
        /// <param name="nodeIndex">Index of node being hulled.</param>
        /// <param name="sides">Number of sides per strut.</param>
        /// <param name="tol">The tolerance (RhinoDoc.ActiveDoc.ModelAbsoluteTolerance is a good bet).</param>
        /// <param name="cleanPlates">If true, the plate faces will be removed from the hull, so that the sleeves can be directly attached.</param>
        /// <remarks>
        /// If a plate point is coplanar with another plate, the hull may be impossible to clean.
        /// This is because the hulling process may remove the coplanar point and create a new face.
        /// </remarks>
        public Mesh MakeConvexHull(int nodeIndex, int sides, double tol, bool cleanPlates)
        {
            Mesh hullMesh = new Mesh();
            ExoHull node = this.Hulls[nodeIndex];
            double radius = node.AvgRadius;

            double planeTolerance = tol * radius / 10;

            // Collect all hull points (i.e. all plate points at the node)
            List<Point3d> pts = new List<Point3d>();
            foreach (int pIndex in node.PlateIndices) pts.AddRange(this.Plates[pIndex].Vtc);

            // 1. Create initial tetrahedron.
            // Form triangle from 3 first points (lie on same plate, thus, same plane)
            hullMesh.Vertices.Add(pts[0]);
            hullMesh.Vertices.Add(pts[1]);
            hullMesh.Vertices.Add(pts[2]);
            Plane planeStart = new Plane(pts[0], pts[1], pts[2]);
            // Form tetrahedron with a 4th point which does not lie on the same plane
            int nextIndex = sides + 1;
            while (Math.Abs(planeStart.DistanceTo(pts[nextIndex])) < planeTolerance)
                nextIndex++;
            hullMesh.Vertices.Add(pts[nextIndex]);
            // Stitch faces of tetrahedron
            hullMesh.Faces.AddFace(0, 2, 1);
            hullMesh.Faces.AddFace(0, 3, 2);
            hullMesh.Faces.AddFace(0, 1, 3);
            hullMesh.Faces.AddFace(1, 2, 3);

            // 2. Begin the incremental hulling process
            // Remove points already checked
            pts.RemoveAt(nextIndex);
            pts.RemoveRange(0, 3);

            // Loop through the remaining points
            for (int i = 0; i < pts.Count; i++)
            {
                MeshTools.NormaliseMesh(ref hullMesh);

                // Find visible faces
                List<int> seenFaces = new List<int>();
                for (int faceIndex = 0; faceIndex < hullMesh.Faces.Count; faceIndex++)
                {
                    Vector3d testVect = pts[i] - hullMesh.Faces.GetFaceCenter(faceIndex);
                    double angle = Vector3d.VectorAngle(hullMesh.FaceNormals[faceIndex], testVect);
                    Plane planeTest = new Plane(hullMesh.Faces.GetFaceCenter(faceIndex), hullMesh.FaceNormals[faceIndex]);
                    if (angle < Math.PI * 0.5 || Math.Abs(planeTest.DistanceTo(pts[i])) < planeTolerance) { seenFaces.Add(faceIndex); }
                }

                // Remove visible faces
                hullMesh.Faces.DeleteFaces(seenFaces);
                // Add current point
                hullMesh.Vertices.Add(pts[i]);

                List<MeshFace> addFaces = new List<MeshFace>();
                // Close open hull based on new vertex
                for (int edgeIndex = 0; edgeIndex < hullMesh.TopologyEdges.Count; edgeIndex++)
                {
                    if (!hullMesh.TopologyEdges.IsSwappableEdge(edgeIndex))
                    {
                        IndexPair V = hullMesh.TopologyEdges.GetTopologyVertices(edgeIndex);
                        int I1 = hullMesh.TopologyVertices.MeshVertexIndices(V.I)[0];
                        int I2 = hullMesh.TopologyVertices.MeshVertexIndices(V.J)[0];
                        addFaces.Add(new MeshFace(I1, I2, hullMesh.Vertices.Count - 1));
                    }
                }
                hullMesh.Faces.AddFaces(addFaces);
            }

            MeshTools.NormaliseMesh(ref hullMesh);


            // 3. If requested, delete the hull faces that lie on the plates (so sleeves can connect directly to the hulls)
            if (cleanPlates)
            {
                List<int> deleteFaces = new List<int>();
                foreach (int plateIndx in node.PlateIndices)
                {
                    List<Point3f> plateVtc = MeshTools.Point3dToPoint3f(this.Plates[plateIndx].Vtc);
                    // recall that strut plates have 'sides+1' vertices.
                    // if the plate has only 'sides' vertices, it is an extra plate (for acute nodes), so we should keep it
                    if (plateVtc.Count < sides + 1) continue;

                    for (int j = 0; j < hullMesh.Faces.Count; j++)
                    {
                        Point3f ptA, ptB, ptC, ptD;
                        hullMesh.Faces.GetFaceVertices(j, out ptA, out ptB, out ptC, out ptD);

                        // check if the mesh face has vertices that belong to a single plate, if so we need to remove the face
                        int matches = 0;
                        foreach (Point3f testPt in plateVtc)
                            if (testPt.EpsilonEquals(ptA, (float)tol) || testPt.EpsilonEquals(ptB, (float)tol) || testPt.EpsilonEquals(ptC, (float)tol))
                                matches++;
                        // if all three face vertices are plate vertices, we should remove the face
                        if (matches == 3)
                            deleteFaces.Add(j);
                    }
                }
                // Remove the faces. Reverse the list so that it is in decreasing order.
                deleteFaces.Reverse();
                foreach (int faceIndx in deleteFaces) hullMesh.Faces.RemoveAt(faceIndx);
            }
            return hullMesh;
        }
        /// <summary>
        /// Construts endface mesh (for single strut nodes).
        /// </summary>
        /// <param name="nodeIndex">Index of the node where the endface should be generated.</param>
        /// <param name="sides">Number of strut sides.</param>
        public Mesh MakeEndFace(int nodeIndex, int sides)
        {
            Mesh endMesh = new Mesh();
            // Set vertices
            foreach (Point3d platePoint in this.Plates[this.Hulls[nodeIndex].PlateIndices[0]].Vtc)
                endMesh.Vertices.Add(platePoint);
            // Stitch faces
            for (int i = 1; i < sides; i++)
                endMesh.Faces.AddFace(0, i, i + 1);
            endMesh.Faces.AddFace(0, sides, 1); // last face wraps

            return endMesh;
        }
        #endregion
    }

    class ExoHull
    {
        #region Fields
        private Point3d m_point3d;
        private List<int> m_sleeveIndices;
        private List<int> m_plateIndices;
        private double m_avgRadius;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ExoHull()
        {
            m_point3d = Point3d.Unset;
            m_sleeveIndices = new List<int>();
            m_plateIndices = new List<int>();
            m_avgRadius = 0.0;
        }
        /// <summary>
        /// Instance constructor based on a Point3d location.
        /// </summary>
        /// <param name="point3d"></param>
        public ExoHull(Point3d point3d)
        {
            m_point3d = point3d;
            m_sleeveIndices = new List<int>();
            m_plateIndices = new List<int>();
            m_avgRadius = 0.0;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Coordinates of node.
        /// </summary>
        public Point3d Point3d
        {
            get { return m_point3d; }
            set { m_point3d = value; }
        }
        /// <summary>
        /// Indices of the sleeves associated to this hull.
        /// </summary>
        public List<int> SleeveIndices
        {
            get { return m_sleeveIndices; }
            set { m_sleeveIndices = value; }
        }
        /// <summary>
        /// Indices of the plates associated to this hull. (parallel to SleeveIndices)
        /// </summary>
        public List<int> PlateIndices
        {
            get { return m_plateIndices; }
            set { m_plateIndices = value; }
        }
        /// <summary>
        /// Average radius at the node, used primarly for extra plates at sharp nodes.
        /// </summary>
        public double AvgRadius
        {
            get { return m_avgRadius; }
            set { m_avgRadius = value; }
        }
        #endregion

        #region Methods
        // none yet
        #endregion
    }

    class ExoSleeve
    {
        #region Fields
        private Curve m_curve;
        private IndexPair m_hullPair;
        private IndexPair m_platePair;
        private double m_startRadius;
        private double m_endRadius;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ExoSleeve()
        {
            m_curve = null;
            m_hullPair = new IndexPair();
            m_platePair = new IndexPair();
            m_startRadius = 0.0;
            m_endRadius = 0.0;
        }
        /// <summary>
        /// Instance constuctor based on the underlying curve for this sleeve.
        /// </summary>
        public ExoSleeve(Curve curve)
        {
            m_curve = curve;
            m_hullPair = new IndexPair();
            m_platePair = new IndexPair();
            m_startRadius = 0.0;
            m_endRadius = 0.0;
        }
        /// <summary>
        /// Instance constuctor based on the underlying curve and hull pair for this sleeve.
        /// </summary>
        public ExoSleeve(Curve curve, IndexPair hullPair)
        {
            m_curve = curve;
            m_hullPair = hullPair;
            m_platePair = new IndexPair();
            m_startRadius = 0.0;
            m_endRadius = 0.0;
        }
        #endregion

        #region Properties
         /// <summary>
        /// The sleeve's underlying curve. (may be linear)
        /// </summary>
        public Curve Curve
        {
            get { return m_curve; }
            set { m_curve = value; }
        }
        /// <summary>
        /// The pair of hull indices for this sleeve.
        /// </summary>
        public IndexPair HullPair
        {
            get { return m_hullPair; }
            set { m_hullPair = value; }
        }
        /// <summary>
        /// The pair of plate indices for this sleeve.
        /// </summary>
        public IndexPair PlatePair
        {
            get { return m_platePair; }
            set { m_platePair = value; }
        }
        /// <summary>
        /// The start radius of the sleeve.
        /// </summary>
        public double StartRadius
        {
            get { return m_startRadius; }
            set { m_startRadius = value; }
        }
        /// <summary>
        /// The end radius of the sleeve.
        /// </summary>
        public double EndRadius
        {
            get { return m_endRadius; }
            set { m_endRadius = value; }
        }
        /// <summary>
        /// The average radius of the sleeve.
        /// </summary>
        public double AvgRadius
        {
            get { return (StartRadius + EndRadius) / 2; }
        }
        #endregion

        #region Methods
        // none yet
        #endregion
    }

    class ExoPlate
    {
        #region Fields
        private double m_offset;
        private Vector3d m_normal;
        private List<Point3d> m_vtc;
        private int m_hullIndex;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ExoPlate()
        {
            m_offset = 0;
            m_normal = Vector3d.Unset;
            m_vtc = new List<Point3d>();
            m_hullIndex = 0;
        }
        /// <summary>
        /// Instance constructor based on the hull index and starting normal for this plate.
        /// </summary>
        public ExoPlate(int hullIndex, Vector3d normal)
        {
            m_offset = 0;
            m_normal = normal;
            m_vtc = new List<Point3d>();
            m_hullIndex = hullIndex;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The offset from the hull's center point (lattice node).
        /// </summary>
        public double Offset
        {
            get { return m_offset; }
            set { m_offset = value; }
        }
        /// <summary>
        /// The normal of the strut at the node. (used for extra plate at sharp nodes)
        /// </summary>
        public Vector3d Normal
        {
            get { return m_normal; }
            set { m_normal = value; }
        }
        /// <summary>
        /// The vertices on the plate. Note that Vtc[0] should be the centerpoint of the plate.
        /// </summary>
        public List<Point3d> Vtc
        {
            get { return m_vtc; }
            set { m_vtc = value; }
        }
        /// <summary>
        /// The index of the parent hull.
        /// </summary>
        public int HullIndex
        {
            get { return m_hullIndex; }
            set { m_hullIndex = value; }
        }
        #endregion
    }
}
