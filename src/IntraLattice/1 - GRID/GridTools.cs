using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Collections;
using Grasshopper.Kernel.Types.Transforms;

// This is a set of methods used by the frame components
// =====================================================
//      Nothing yet

// Written by Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class TopologyTools
    {
        /// <summary>
        /// Converts a list of lines
        /// </summary>
        public static void NormaliseTopology(ref Point3dList nodes)
        {
            // We'll build the bounding box as well
            var xRange = new Interval();
            var yRange = new Interval();
            var zRange = new Interval();

            // Get the bounding box size (check for extreme values)
            foreach (Point3d node in nodes)
            {
                if (node.X < xRange.T0) xRange.T0 = node.X;
                if (node.X > xRange.T1) xRange.T1 = node.X;
                if (node.Y < yRange.T0) yRange.T0 = node.Y;
                if (node.Y > yRange.T1) yRange.T1 = node.Y;
                if (node.Z < zRange.T0) zRange.T0 = node.Z;
                if (node.Z > zRange.T1) zRange.T1 = node.Z;
            }

            // move bounding box to origin
            Vector3d toOrigin = new Vector3d(-xRange.T0, -yRange.T0, -zRange.T0);
            nodes.Transform(Transform.Translation(toOrigin));
            // normalise to 1x1x1 bounding box
            nodes.Transform(Transform.Scale(Plane.WorldXY, 1 / xRange.Length, 1 / yRange.Length, 1 / zRange.Length));

        }

        /// <summary>
        /// Converts list of lines into a list of unique nodes and a list of struts (struts as pairs of node indices)
        /// </summary>
        public static void Topologize(ref List<Curve> lines, ref Point3dList nodes, ref List<IndexPair> struts)
        {
            // Iterate through list of lines
            foreach (Curve line in lines)
            {
                // Get line, and it's endpoints
                Point3d[] pts = new Point3d[] { line.PointAtStart, line.PointAtEnd };
                List<int> nodeIndex = new List<int>();

                // Loop over end points, being sure to not create the same node twice
                foreach (Point3d endPt in pts)
                {
                    int closestIndex = nodes.ClosestIndex(endPt);  // find closest node to current pt
                    // If node already exists
                    if (nodes.Count != 0 && nodes[closestIndex].DistanceTo(endPt) < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                        nodeIndex.Add(closestIndex);
                    // If it doesn't exist, add it
                    else
                    {
                        nodes.Add(endPt);
                        nodeIndex.Add(nodes.Count - 1);
                    }
                }

                // Now we save the strut (as pair of node indices)
                struts.Add(new IndexPair(nodeIndex[0], nodeIndex[1]));
            }
        }



    }
}