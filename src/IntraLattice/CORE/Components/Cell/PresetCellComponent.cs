using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino;
using IntraLattice.Properties;
using IntraLattice.CORE.Components;
using IntraLattice.CORE.Helpers;
using IntraLattice.CORE.Data.GH_Goo;
using IntraLattice.CORE.Data;

// Summary:     This component can generate a selection of pre-defined unit cell topologies
// ===============================================================================
// Details:     - Selection menu is automatically generated (if you add a topology, make sure to add it to the selection menu, in InputTools.TopoSelect()
//              - The cells don't need to be unitized (bounding box 1x1x1) or at the origin.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice.CORE.Components
{
    public class PresetCellComponent : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the PresetCellComponent class.
        /// </summary>
        public PresetCellComponent()
            : base("Preset Cell", "PresetCell",
                "Built-in selection of unit cell topologies.",
                "IntraLattice", "Cell")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Cell Tye", "Type", "Unit cell topology type", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Topology", "Topo", "Unit cell topology", GH_ParamAccess.item);
            pManager.AddLineParameter("Lines", "L", "Optional output so you can modify the unit cell lines. Pass through the CustomCell component when you're done.", GH_ParamAccess.list);
            pManager.HideParameter(1);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 0. Generate input menu list
            Component = this;
            GrasshopperDocument = this.OnPingDocument();
            // Only generate it if the input has no source
            if (Component.Params.Input[0].SourceCount == 0)
            {
                InputTools.TopoSelect(ref Component, ref GrasshopperDocument, 0, 11);
            }

            // 1. Retrieve input
            int cellType = 0;
            if (!DA.GetData(0, ref cellType)) { return; }

            // 2. Instantiate line list
            var lines = new List<Line>();

            // 3. Set cell size
            double d = 5;

            // 4. Switch statement for the different cell types
            switch (cellType)
            {
                // "GRID"
                case 0:
                    lines = GridLines(d);
                    break;
                // "X"
                case 1:
                    lines = XLines(d);
                    break;
                // "STAR"
                case 2:
                    lines = StarLines(d);
                    break;
                // "CROSS"
                case 3:
                    lines = CrossLines(d);                        
                    break;
                // "TESSERACT"
                case 4:
                    lines = TesseractLines(d);
                    break;
                // "VINTILES"
                case 5:
                    lines = VintileLines(d);
                    break;
                // "OCTET"
                case 6:
                    lines = OctetLines(d);
                    break;
                // "DIAMOND"
                case 7:
                    lines = DiamondLines(d);
                    break;
                // "HONEYCOMB"
                case 8:
                    lines = Honeycomb(d);
                    break;
                // "AUXETIC HONEYCOMB"
                case 9:
                    lines = AuxeticHoneycomb(d);
                    break;
            }

            // 5. Instantiate UnitCell object and check validity.
            var cell = new UnitCell(lines);
            if (!cell.isValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid cell - this is embarassing.");
            }

            // 6. Set output (as LatticeCellGoo)
            DA.SetData(0, new UnitCellGoo(cell));
            DA.SetDataList(1, lines);
        }

        #region Line Generation Methods
        private List<Line> GridLines(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // Generate nodes
            CellTools.MakeCornerNodes(ref nodes, d);
            // Generate struts
            foreach (int i in (new int[3] { 1, 3, 4 }))
            {
                lines.Add(new Line(nodes[0], nodes[i]));
            }
            foreach (int i in (new int[3] { 1, 3, 6 }))
            {
                lines.Add(new Line(nodes[2], nodes[i]));
            }
            foreach (int i in (new int[3] { 1, 4, 6 }))
            {
                lines.Add(new Line(nodes[5], nodes[i]));
            }
            foreach (int i in (new int[3] { 3, 4, 6 }))
            {
                lines.Add(new Line(nodes[7], nodes[i]));
            }

            return lines;
        }

        private List<Line> XLines(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // Generate nodes
            CellTools.MakeCornerNodes(ref nodes, d);
            // Generate struts
            lines.Add(new Line(nodes[0], nodes[6]));
            lines.Add(new Line(nodes[1], nodes[7]));
            lines.Add(new Line(nodes[3], nodes[5]));
            lines.Add(new Line(nodes[2], nodes[4]));

            return lines;
        }

        private List<Line> StarLines(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // Generate nodes
            CellTools.MakeCornerNodes(ref nodes, d);
            // Generate struts
            lines.Add(new Line(nodes[0], nodes[6]));
            lines.Add(new Line(nodes[1], nodes[7]));
            lines.Add(new Line(nodes[3], nodes[5]));
            lines.Add(new Line(nodes[2], nodes[4]));
            foreach (int i in (new int[3] { 1, 3, 4 }))
            {
                lines.Add(new Line(nodes[0], nodes[i])); 
            }
            foreach (int i in (new int[3] { 1, 3, 6 }))
            {
                lines.Add(new Line(nodes[2], nodes[i]));
            }
            foreach (int i in (new int[3] { 1, 4, 6 }))
            {
                lines.Add(new Line(nodes[5], nodes[i]));
            }
            foreach (int i in (new int[3] { 3, 4, 6 }))
            {
                lines.Add(new Line(nodes[7], nodes[i]));
            }

            return lines;
        }

        private List<Line> DiamondLines(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            //corner points
            nodes.Add(new Point3d(0, 0, 0));
            // face-centered points
            nodes.Add(new Point3d(0, d / 2, d / 2));
            nodes.Add(new Point3d(d / 2, 0 , d / 2));
            nodes.Add(new Point3d(d / 2, d / 2, 0));
            // others
            nodes.Add(new Point3d(d/4, d/4, d/4));

            lines.Add(new Line(nodes[4], nodes[0]));
            lines.Add(new Line(nodes[4], nodes[1]));
            lines.Add(new Line(nodes[4], nodes[2]));
            lines.Add(new Line(nodes[4], nodes[3]));

            var lines2 = new List<Line>(lines);
            foreach (var line in lines)
            {
                var newLine = new Line(line.From, line.To);
                newLine.Transform(Transform.Translation(d / 2, d / 2, 0));
                lines2.Add(newLine);
            }
            foreach (var line in lines)
            {
                var newLine = new Line(line.From, line.To);
                newLine.Transform(Transform.Rotation(Math.PI / 2, nodes[4]));
                newLine.Transform(Transform.Translation(d / 2, d / 2, d/2));
                lines2.Add(newLine);
            }
            foreach (var line in lines)
            {
                var newLine = new Line(line.From, line.To);
                newLine.Transform(Transform.Rotation(Math.PI / 2, nodes[4]));
                newLine.Transform(Transform.Translation(0, 0, d / 2));
                lines2.Add(newLine);
            }
            
            lines.AddRange(lines2);

            return lines;
        }

        private List<Line> CrossLines(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // Generate nodes
            CellTools.MakeCornerNodes(ref nodes, d);
            // Generate struts
            foreach (int i in (new int[2] { 5, 7 }))
            {
                lines.Add(new Line(nodes[0], nodes[i]));
                lines.Add(new Line(nodes[2], nodes[i]));
            }
            foreach (int i in (new int[2] { 4, 6 }))
            {
                lines.Add(new Line(nodes[1], nodes[i]));
                lines.Add(new Line(nodes[3], nodes[i]));
            }
            foreach (int i in (new int[4] { 0, 1, 4, 5 }))
            {
                lines.Add(new Line(nodes[i], nodes[i + 2]));
            }

            return lines;
        }

        private List<Line> TesseractLines(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // Generate nodes
            CellTools.MakeCornerNodes(ref nodes, d);          
            nodes.Add(new Point3d(d / 4, d / 4, d / 4));
            nodes.Add(new Point3d(3 * d / 4, d / 4, d / 4));
            nodes.Add(new Point3d(3 * d / 4, 3 * d / 4, d / 4));
            nodes.Add(new Point3d(d / 4, 3 * d / 4, d / 4));
            nodes.Add(new Point3d(d / 4, d / 4, 3 * d / 4));
            nodes.Add(new Point3d(3 * d / 4, d / 4, 3 * d / 4));
            nodes.Add(new Point3d(3 * d / 4, 3 * d / 4, 3 * d / 4));
            nodes.Add(new Point3d(d / 4, 3 * d / 4, 3 * d / 4));
            // Generate struts
            foreach (int i in (new int[3] { 1, 3, 4 }))
            {
                lines.Add(new Line(nodes[0], nodes[i]));
                lines.Add(new Line(nodes[8], nodes[i + 8]));
            }
            foreach (int i in (new int[3] { 1, 3, 6 }))
            {
                lines.Add(new Line(nodes[2], nodes[i]));
                lines.Add(new Line(nodes[10], nodes[i + 8]));
            }
            foreach (int i in (new int[3] { 1, 4, 6 }))
            {
                lines.Add(new Line(nodes[5], nodes[i]));
                lines.Add(new Line(nodes[13], nodes[i + 8]));
            }
            foreach (int i in (new int[3] { 3, 4, 6 }))
            {
                lines.Add(new Line(nodes[7], nodes[i]));
                lines.Add(new Line(nodes[15], nodes[i + 8]));
            }
            for (int i = 0; i < 8; i++)
                lines.Add(new Line(nodes[i], nodes[i + 8]));

            return lines;
        }

        private List<Line> VintileLines(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // Generate nodes
            foreach (double z in new double[2] { 0, d })
            {
                nodes.Add(new Point3d(0, d / 4, z));
                nodes.Add(new Point3d(0, 3 * d / 4, z));
                nodes.Add(new Point3d(d / 4, d, z));
                nodes.Add(new Point3d(3 * d / 4, d, z));
                nodes.Add(new Point3d(d, 3 * d / 4, z));
                nodes.Add(new Point3d(d, d / 4, z));
                nodes.Add(new Point3d(3 * d / 4, 0, z));
                nodes.Add(new Point3d(d / 4, 0, z));
            }
            foreach (double z in new double[2] { d / 4, 3 * d / 4 })
            {
                nodes.Add(new Point3d(0, d / 2, z));
                nodes.Add(new Point3d(d / 2, d, z));
                nodes.Add(new Point3d(d, d / 2, z));
                nodes.Add(new Point3d(d / 2, 0, z));
            }
            foreach (double y in new double[2] { d / 4, 3 * d / 4 })
            {
                nodes.Add(new Point3d(d / 2, y, d / 2));
            }
            foreach (double x in new double[2] { d / 4, 3 * d / 4 })
            {
                nodes.Add(new Point3d(x, d / 2, d / 2));
            }
            // Generate struts
            foreach (int i in new int[3] { 0, 1, 26 })
            {
                lines.Add(new Line(nodes[16], nodes[i]));
            }
            foreach (int i in new int[3] { 2, 3, 25 })
            {
                lines.Add(new Line(nodes[17], nodes[i]));
            }
            foreach (int i in new int[3] { 4, 5, 27 })
            {
                lines.Add(new Line(nodes[18], nodes[i]));
            }
            foreach (int i in new int[3] { 6, 7, 24 })
            {
                lines.Add(new Line(nodes[19], nodes[i]));
            }
            foreach (int i in new int[3] { 8, 9, 26 })
            {
                lines.Add(new Line(nodes[20], nodes[i]));
            }
            foreach (int i in new int[3] { 10, 11, 25 })
            {
                lines.Add(new Line(nodes[21], nodes[i]));
            }
            foreach (int i in new int[3] { 12, 13, 27 })
            {
                lines.Add(new Line(nodes[22], nodes[i]));
            }
            foreach (int i in new int[3] { 14, 15, 24 })
            {
                lines.Add(new Line(nodes[23], nodes[i]));
            }
            foreach (int i in new int[6] { 1, 3, 5, 9, 11, 13 })
            {
                lines.Add(new Line(nodes[i], nodes[i + 1]));
            }
            foreach (int i in new int[2] { 24, 25 })
            {
                lines.Add(new Line(nodes[26], nodes[i]));
                lines.Add(new Line(nodes[27], nodes[i]));
            }
            lines.Add(new Line(nodes[0], nodes[7]));
            lines.Add(new Line(nodes[8], nodes[15]));

            return lines;
        }

        private List<Line> OctetLines(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // Generate nodes
            CellTools.MakeCornerNodes(ref nodes, d);
            nodes.Add(new Point3d(d, d / 2, d / 2));
            nodes.Add(new Point3d(d / 2, d, d / 2));
            nodes.Add(new Point3d(0, d / 2, d / 2));
            nodes.Add(new Point3d(d / 2, 0, d / 2));
            nodes.Add(new Point3d(d / 2, d / 2, 0));
            nodes.Add(new Point3d(d / 2, d / 2, d));
            // Generate struts
            foreach (int i in new int[8] { 0, 1, 2, 3, 8, 9, 10, 11 })
            {
                lines.Add(new Line(nodes[12], nodes[i]));
            }
            foreach (int i in new int[8] { 4, 5, 6, 7, 8, 9, 10, 11 })
            {
                lines.Add(new Line(nodes[13], nodes[i]));
            }
            foreach (int i in new int[4] { 0, 3, 4, 7 })
            {
                lines.Add(new Line(nodes[10], nodes[i]));
            }
            foreach (int i in new int[6] { 2, 3, 6, 7, 8, 10 })
            {
                lines.Add(new Line(nodes[9], nodes[i]));
            }
            foreach (int i in new int[4] { 1, 2, 5, 6 })
            {
                lines.Add(new Line(nodes[8], nodes[i]));
            }
            foreach (int i in new int[6] { 0, 1, 4, 5, 8, 10 })
            {
                lines.Add(new Line(nodes[11], nodes[i]));
            }

            return lines;
        }

        private List<Line> Honeycomb(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // This is a bit messy, but I'm too lazy for elegance right now
            // First, we loop to create the same set of nodes on two parallel faces
            for (int i = 0; i < 2; i++ )
            {
                double y = 3 * d * i;
                nodes.Add(new Point3d(2.25 * d, y, 2 * d));
                nodes.Add(new Point3d(2.25 * d, y,  d));
                nodes.Add(new Point3d(0.75 * d, y, 2 * d));
                nodes.Add(new Point3d(0.75 * d, y, d));

                nodes.Add(new Point3d(0, y, 0));
                nodes.Add(new Point3d(0, y, 0.5*d));
                nodes.Add(new Point3d(0, y, 2.5 * d));
                nodes.Add(new Point3d(0, y, 3 * d));

                nodes.Add(new Point3d(1.5 * d, y, 0));
                nodes.Add(new Point3d(1.5 * d, y, 0.5 * d));
                nodes.Add(new Point3d(1.5 * d, y, 2.5 * d));
                nodes.Add(new Point3d(1.5 * d, y, 3 * d));

                nodes.Add(new Point3d(3 * d, y, 0));
                nodes.Add(new Point3d(3 * d, y, 0.5 * d));
                nodes.Add(new Point3d(3 * d, y, 2.5 * d));
                nodes.Add(new Point3d(3 * d, y, 3 * d));
            }

            // Create both faces
            for (int i = 0; i < 2; i++ )
            {
                int indexOffset = i * 16;
                foreach (int j in new int[3] { 2, 5, 9 })
                {
                    lines.Add(new Line(nodes[3 + indexOffset], nodes[j + indexOffset]));
                }
                foreach (int j in new int[3] { 0,9,13 })
                {
                    lines.Add(new Line(nodes[1 + indexOffset], nodes[j + indexOffset]));
                }
                foreach (int j in new int[3] { 0, 2, 11 })
                {
                    lines.Add(new Line(nodes[10 + indexOffset], nodes[j + indexOffset]));
                }
                lines.Add(new Line(nodes[6 + indexOffset], nodes[7 + indexOffset]));
                lines.Add(new Line(nodes[14 + indexOffset], nodes[15 + indexOffset]));
                lines.Add(new Line(nodes[4 + indexOffset], nodes[5 + indexOffset]));
                lines.Add(new Line(nodes[8 + indexOffset], nodes[9 + indexOffset]));
                lines.Add(new Line(nodes[13 + indexOffset], nodes[12 + indexOffset]));
                lines.Add(new Line(nodes[0 + indexOffset], nodes[14 + indexOffset]));
                lines.Add(new Line(nodes[2 + indexOffset], nodes[6 + indexOffset]));
            }

            // Create interface lines
            for (int i = 0; i < 16; i++ )
            {
                lines.Add(new Line(nodes[i], nodes[i + 16]));
            }

            return lines;
        }
        private List<Line> AuxeticHoneycomb(double d)
        {
            var lines = new List<Line>();
            var nodes = new List<Point3d>();

            // This is a bit messy, but I'm too lazy for elegance right now
            // First, we loop to create the same set of nodes on two parallel faces
            for (int i = 0; i < 2; i++)
            {
                double y = 3 * d * i;
                nodes.Add(new Point3d(2.25 * d, y, 2.5 * d));
                nodes.Add(new Point3d(2.25 * d, y, 0.5 * d));
                nodes.Add(new Point3d(0.75 * d, y, 2.5 * d));
                nodes.Add(new Point3d(0.75 * d, y, 0.5 * d));

                nodes.Add(new Point3d(0, y, 0));
                nodes.Add(new Point3d(0, y, d));
                nodes.Add(new Point3d(0, y, 2 * d));
                nodes.Add(new Point3d(0, y, 3 * d));

                nodes.Add(new Point3d(1.5 * d, y, 0));
                nodes.Add(new Point3d(1.5 * d, y, d));
                nodes.Add(new Point3d(1.5 * d, y, 2 * d));
                nodes.Add(new Point3d(1.5 * d, y, 3 * d));

                nodes.Add(new Point3d(3 * d, y, 0));
                nodes.Add(new Point3d(3 * d, y, d));
                nodes.Add(new Point3d(3 * d, y, 2 * d));
                nodes.Add(new Point3d(3 * d, y, 3 * d));
            }

            // Now create struts for each face
            for (int i = 0; i < 2; i++)
            {
                int indexOffset = i * 16;
                foreach (int j in new int[3] { 2, 5, 9 })
                {
                    lines.Add(new Line(nodes[3 + indexOffset], nodes[j + indexOffset]));
                }
                foreach (int j in new int[3] { 0, 9, 13 })
                {
                    lines.Add(new Line(nodes[1 + indexOffset], nodes[j + indexOffset]));
                }
                foreach (int j in new int[3] { 0, 2, 11 })
                {
                    lines.Add(new Line(nodes[10 + indexOffset], nodes[j + indexOffset]));
                }
                lines.Add(new Line(nodes[6 + indexOffset], nodes[7 + indexOffset]));
                lines.Add(new Line(nodes[14 + indexOffset], nodes[15 + indexOffset]));
                lines.Add(new Line(nodes[4 + indexOffset], nodes[5 + indexOffset]));
                lines.Add(new Line(nodes[8 + indexOffset], nodes[9 + indexOffset]));
                lines.Add(new Line(nodes[13 + indexOffset], nodes[12 + indexOffset]));
                lines.Add(new Line(nodes[0 + indexOffset], nodes[14 + indexOffset]));
                lines.Add(new Line(nodes[2 + indexOffset], nodes[6 + indexOffset]));
            }

            // Create struts between faces
            for (int i = 0; i < 16; i++)
            {
                lines.Add(new Line(nodes[i], nodes[i + 16]));
            }

            return lines;
        }
        #endregion

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
                //You can add image files to your project resources and access them like this:
                return Resources.presetCell;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{508cc705-bc5b-42a9-8100-c1e364f3b83d}"); }
        }
    }
}