using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino;
using IntraLattice.Properties;

// Summary:     This component can generate a selection of pre-defined unit cell topologies
// ===============================================================================
// Details:     - Selection menu is automatically generated (if you add a topology, make sure to add it to the selection menu, in InputTools.TopoSelect()
//              - The cells don't need to be unitized (bounding box 1x1x1) or at the origin, the framing components are responsible of this
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class PresetCell : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the PresetCell class.
        /// </summary>
        public PresetCell()
            : base("PresetCell", "PresetCell",
                "Built-in selection of unit cell topologies.",
                "IntraLattice2", "Cell")
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
            pManager.AddLineParameter("Topology", "Topo", "Line topology", GH_ParamAccess.list);
            pManager.AddPointParameter("Topology", "Topo", "Line topology", GH_ParamAccess.list);
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
            if (Component.Params.Input[0].SourceCount == 0) InputTools.TopoSelect(ref Component, ref GrasshopperDocument, 0, 11);

            // 1. Retrieve input
            int cellType = 0;
            if (!DA.GetData(0, ref cellType)) { return; }

            // 2. Instantiate lists
            var nodes = new List<Point3d>();
            var lines = new List<Line>();

            // Set cell size
            double d = 5;

            // Switch statement for the different cell types
            switch (cellType)
            {
                // "GRID"
                case 0:
                    // generate nodes
                    nodes.Add(new Point3d(0, 0, 0));
                    nodes.Add(new Point3d(d, 0, 0));
                    nodes.Add(new Point3d(d, d, 0));
                    nodes.Add(new Point3d(0, d, 0));
                    nodes.Add(new Point3d(0, 0, d));
                    nodes.Add(new Point3d(d, 0, d));
                    nodes.Add(new Point3d(d, d, d));
                    nodes.Add(new Point3d(0, d, d));
                    // generate struts
                    foreach (int i in (new int[3] { 1, 3, 4 }))
                        lines.Add(new Line(nodes[0], nodes[i]));
                    foreach (int i in (new int[3] { 1, 3, 6 }))
                        lines.Add(new Line(nodes[2], nodes[i]));
                    foreach (int i in (new int[3] { 1, 4, 6 }))
                        lines.Add(new Line(nodes[5], nodes[i]));
                    foreach (int i in (new int[3] { 3, 4, 6 }))
                        lines.Add(new Line(nodes[7], nodes[i]));
                    break;
                
                // "X"
                case 1:
                    // generate nodes
                    nodes.Add(new Point3d(0, 0, 0));
                    nodes.Add(new Point3d(d, 0, 0));
                    nodes.Add(new Point3d(d, d, 0));
                    nodes.Add(new Point3d(0, d, 0));
                    nodes.Add(new Point3d(0, 0, d));
                    nodes.Add(new Point3d(d, 0, d));
                    nodes.Add(new Point3d(d, d, d));
                    nodes.Add(new Point3d(0, d, d));
                    // generate struts
                    lines.Add(new Line(nodes[0], nodes[6]));
                    lines.Add(new Line(nodes[1], nodes[7]));
                    lines.Add(new Line(nodes[3], nodes[5]));
                    lines.Add(new Line(nodes[2], nodes[4]));
                    break;

                // "STAR"
                case 2:
                    // generate nodes
                    nodes.Add(new Point3d(0, 0, 0));
                    nodes.Add(new Point3d(d, 0, 0));
                    nodes.Add(new Point3d(d, d, 0));
                    nodes.Add(new Point3d(0, d, 0));
                    nodes.Add(new Point3d(0, 0, d));
                    nodes.Add(new Point3d(d, 0, d));
                    nodes.Add(new Point3d(d, d, d));
                    nodes.Add(new Point3d(0, d, d));
                    // generate struts
                    lines.Add(new Line(nodes[0], nodes[6]));
                    lines.Add(new Line(nodes[1], nodes[7]));
                    lines.Add(new Line(nodes[3], nodes[5]));
                    lines.Add(new Line(nodes[2], nodes[4]));
                    foreach (int i in (new int[3] { 1, 3, 4 }))
                        lines.Add(new Line(nodes[0], nodes[i]));
                    foreach (int i in (new int[3] { 1, 3, 6 }))
                        lines.Add(new Line(nodes[2], nodes[i]));
                    foreach (int i in (new int[3] { 1, 4, 6 }))
                        lines.Add(new Line(nodes[5], nodes[i]));
                    foreach (int i in (new int[3] { 3, 4, 6 }))
                        lines.Add(new Line(nodes[7], nodes[i]));
                    break;

                // "CROSS"
                case 3:
                    // generate nodes
                    nodes.Add(new Point3d(0, 0, 0));
                    nodes.Add(new Point3d(d, 0, 0));
                    nodes.Add(new Point3d(d, d, 0));
                    nodes.Add(new Point3d(0, d, 0));
                    nodes.Add(new Point3d(0, 0, d));
                    nodes.Add(new Point3d(d, 0, d));
                    nodes.Add(new Point3d(d, d, d));
                    nodes.Add(new Point3d(0, d, d));
                    // generate struts
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
                        
                    break;
                
                // "TESSERACT"
                case 4:
                    // generate nodes
                    nodes.Add(new Point3d(0, 0, 0));        // outer nodes
                    nodes.Add(new Point3d(d, 0, 0));
                    nodes.Add(new Point3d(d, d, 0));
                    nodes.Add(new Point3d(0, d, 0));
                    nodes.Add(new Point3d(0, 0, d));
                    nodes.Add(new Point3d(d, 0, d));
                    nodes.Add(new Point3d(d, d, d));
                    nodes.Add(new Point3d(0, d, d));
                    nodes.Add(new Point3d(d/4, d/4, d/4));  // inner nodes
                    nodes.Add(new Point3d(3*d/4, d/4, d/4));
                    nodes.Add(new Point3d(3*d/4, 3*d/4, d/4));
                    nodes.Add(new Point3d(d/4, 3*d/4, d/4));
                    nodes.Add(new Point3d(d/4, d/4, 3*d/4));
                    nodes.Add(new Point3d(3*d/4, d/4, 3*d/4));
                    nodes.Add(new Point3d(3*d/4, 3*d/4, 3*d/4));
                    nodes.Add(new Point3d(d/4, 3*d/4, 3*d/4));
                    // generate struts
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
                    break;

                // "VINTILES"
                case 5:
                    // generate nodes
                    foreach (double z in new double[2] { 0, d })
                    {
                        nodes.Add(new Point3d(0, d/4, z));
                        nodes.Add(new Point3d(0, 3*d/4, z));
                        nodes.Add(new Point3d(d/4, d, z));
                        nodes.Add(new Point3d(3*d/4, d, z));
                        nodes.Add(new Point3d(d, 3*d/4, z));
                        nodes.Add(new Point3d(d, d/4, z));
                        nodes.Add(new Point3d(3*d/4, 0, z));
                        nodes.Add(new Point3d(d/4, 0, z));
                    }
                    foreach (double z in new double[2] { d/4, 3*d/4 })
                    {
                        nodes.Add(new Point3d(0, d/2, z));
                        nodes.Add(new Point3d(d/2, d, z));
                        nodes.Add(new Point3d(d, d/2, z));
                        nodes.Add(new Point3d(d/2, 0, z));
                    }
                    foreach (double y in new double[2] { d/4, 3*d/4 })
                        nodes.Add(new Point3d(d/2, y, d/2));
                    foreach (double x in new double[2] { d/4, 3*d/4 })
                        nodes.Add(new Point3d(x, d/2, d/2));
                    // generate struts
                    foreach (int i in new int[3] { 0, 1, 26 })
                        lines.Add(new Line(nodes[16], nodes[i]));
                    foreach (int i in new int[3] { 2, 3, 25 })
                        lines.Add(new Line(nodes[17], nodes[i]));
                    foreach (int i in new int[3] { 4, 5, 27 })
                        lines.Add(new Line(nodes[18], nodes[i]));
                    foreach (int i in new int[3] { 6, 7, 24 })
                        lines.Add(new Line(nodes[19], nodes[i]));
                    foreach (int i in new int[3] { 8, 9, 26 })
                        lines.Add(new Line(nodes[20], nodes[i]));
                    foreach (int i in new int[3] { 10, 11, 25 })
                        lines.Add(new Line(nodes[21],nodes[i]));
                    foreach (int i in new int[3] { 12, 13, 27 })
                        lines.Add(new Line(nodes[22], nodes[i]));
                    foreach (int i in new int[3] { 14, 15, 24 })
                        lines.Add(new Line(nodes[23], nodes[i]));
                    foreach (int i in new int[6] { 1, 3, 5, 9, 11, 13 })
                        lines.Add(new Line(nodes[i], nodes[i + 1]));
                    foreach (int i in new int[2] { 24, 25 })
                    {
                        lines.Add(new Line(nodes[26], nodes[i]));
                        lines.Add(new Line(nodes[27], nodes[i]));
                    }
                    lines.Add(new Line(nodes[0], nodes[7]));
                    lines.Add(new Line(nodes[8], nodes[15]));
                    break;

                // "OCTAHEDRAL"
                case 6:
                    // generate nodes
                    nodes.Add(new Point3d(0, 0, 0));
                    nodes.Add(new Point3d(0, d, 0));
                    nodes.Add(new Point3d(d, d, 0));
                    nodes.Add(new Point3d(d, 0, 0));
                    nodes.Add(new Point3d(0, 0, d));
                    nodes.Add(new Point3d(0, d, d));
                    nodes.Add(new Point3d(d, d, d));
                    nodes.Add(new Point3d(d, 0, d));
                    nodes.Add(new Point3d(d, d/2, d/2));
                    nodes.Add(new Point3d(d/2, d, d/2));
                    nodes.Add(new Point3d(0, d/2, d/2));
                    nodes.Add(new Point3d(d/2, 0, d/2));
                    nodes.Add(new Point3d(d/2, d/2, 0));
                    nodes.Add(new Point3d(d/2, d/2, d));
                    // generate struts
                    foreach(int i in new int[8]{ 0, 1, 2, 3, 8, 9, 10, 11})
                        lines.Add(new Line(nodes[12], nodes[i]));
                    foreach (int i in new int[8] { 4, 5, 6, 7, 8, 9, 10, 11 })
                        lines.Add(new Line(nodes[13], nodes[i]));
                    foreach (int i in new int[4] { 0, 1, 4, 5 })
                        lines.Add(new Line(nodes[10], nodes[i]));
                    foreach (int i in new int[6] { 1, 2, 5, 6, 8, 10 })
                        lines.Add(new Line(nodes[9], nodes[i]));
                    foreach (int i in new int[4] { 2, 3, 6, 7 })
                        lines.Add(new Line(nodes[8], nodes[i]));
                    foreach (int i in new int[6] { 0, 3, 4, 7, 8, 10 })
                        lines.Add(new Line(nodes[11], nodes[i]));
                    break;
            }

            CellTools.FixIntersections(ref lines);

            // 8. Set output
            DA.SetDataList(0, lines);
            DA.SetDataList(1, nodes);
            
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                //return Resources.atom;
                return null;
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