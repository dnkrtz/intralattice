using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Collections;
using Grasshopper.Kernel.Types.Transforms;
using Rhino.Geometry.Intersect;

// Summary:     This class contains static methods used to format unit cells.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Helpers
{
    public class CellTools
    {

        /// <summary>
        /// Explodes lines at intersections. (because all nodes must be defined)
        /// </summary>
        public static void FixIntersections(ref List<Line> lines)
        {
            // Set tolerance
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Check 2 - Fix any intersections, all nodes must be defined
            List<int> linesToRemove = new List<int>();
            List<Line> splitLines = new List<Line>();
            // Loop through all combinations of lines
            for (int a=0; a<lines.Count; a++)
            {
                for (int b=a+1; b<lines.Count; b++)
                {
                    // Line parameter at intersection, for line A and line B
                    double paramA, paramB;
                    bool intersectionFound = Intersection.LineLine(lines[a], lines[b], out paramA, out paramB, tol, true);

                    // If intersection was found
                    if (intersectionFound)
                    {
                        // If intersection isn't start/end point of line A, we split the line
                        if ((paramA > tol) && (1 - paramA > tol) && !linesToRemove.Contains(a))
                        {
                            // Store new split lines, and store the index of the line to remove
                            splitLines.Add(new Line(lines[a].From, lines[a].PointAt(paramA)));
                            splitLines.Add(new Line(lines[a].PointAt(paramA), lines[a].To));
                            linesToRemove.Add(a); 
                        }
                        // Same for line B
                        if ((paramB > tol) && (1-paramB > tol) && !linesToRemove.Contains(b))
                        {
                            splitLines.Add(new Line(lines[b].From, lines[b].PointAt(paramB)));
                            splitLines.Add(new Line(lines[b].PointAt(paramB), lines[b].To));
                            linesToRemove.Add(b);
                        }

                    }
                }
            }
            // Sort and reverse indices because we need to delete list items in decreasing index order
            linesToRemove.Sort();
            linesToRemove.Reverse();
            // Remove lines that were split, and add the new lines
            foreach (int index in linesToRemove) lines.RemoveAt(index);
            lines.AddRange(splitLines);
        }

        /// <summary>
        /// Quick method for generating the corner nodes of a cell.
        /// </summary>
        public static void MakeCornerNodes(ref List<Point3d> nodes, double d)
        {
            nodes.Add(new Point3d(0, 0, 0));
            nodes.Add(new Point3d(d, 0, 0));
            nodes.Add(new Point3d(d, d, 0));
            nodes.Add(new Point3d(0, d, 0));
            nodes.Add(new Point3d(0, 0, d));
            nodes.Add(new Point3d(d, 0, d));
            nodes.Add(new Point3d(d, d, d));
            nodes.Add(new Point3d(0, d, d));
        }
    }

}