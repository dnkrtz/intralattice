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
        /// This method explodes lines at intersections. (because all nodes must be defined)
        /// </summary>
        public static void FixIntersections(ref List<Line> lines)
        {
            // Set tolerance
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Check 2 - Fix any intersections, all nodes must be defined
            List<int> linesToRemove = new List<int>();
            List<Line> splitLines = new List<Line>();
            for (int a=0; a<lines.Count; a++)
            {
                for (int b=a+1; b<lines.Count; b++)
                {
                    double paramA, paramB;
                    bool intersectionFound = Intersection.LineLine(lines[a], lines[b], out paramA, out paramB, tol, true);

                    // if intersection was found
                    if (intersectionFound)
                    {
                        // if intersection isn't start/end point, we split the line
                        if ((paramA > tol) && (1 - paramA > tol) && !linesToRemove.Contains(a))
                        {
                            splitLines.Add(new Line(lines[a].From, lines[a].PointAt(paramA)));
                            splitLines.Add(new Line(lines[a].PointAt(paramA), lines[a].To));
                            linesToRemove.Add(a); // remove old and add new
                        }
                        if ((paramB > tol) && (1-paramB > tol) && !linesToRemove.Contains(b))
                        {
                            splitLines.Add(new Line(lines[b].From, lines[b].PointAt(paramB)));
                            splitLines.Add(new Line(lines[b].PointAt(paramB), lines[b].To));
                            linesToRemove.Add(b); // remove old strut
                        }

                    }
                }
            }
            // remove lines that were split, and add the new lines
            // sort and reverse because we need to delete items in decreasing index order (since a removed item moves following item indices -1)
            linesToRemove.Sort();
            linesToRemove.Reverse();
            foreach (int index in linesToRemove) lines.RemoveAt(index);
            lines.AddRange(splitLines);
        }

    }

}