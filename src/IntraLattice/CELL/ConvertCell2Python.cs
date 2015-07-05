using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Rhino.Geometry;



namespace IntraLattice.CELL
{
    public class ConvertCell2Python : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConvertCell2Python class.
        /// </summary>
        public ConvertCell2Python()
            : base("ConvertCell2Python", "Nickname",
                "Description",
                "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("line", "", "list of line", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("unit line", "", "list of unit line", GH_ParamAccess.list);
        }

        public override void CreateAttributes()
        {
            base.CreateAttributes();
            m_attributes = new GUI(this);
        }

        public bool toggle_switch = false;

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Point> listofpoint = new Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.GH_Point>();
            List<Grasshopper.Kernel.Types.GH_Line> listofline = new List<Grasshopper.Kernel.Types.GH_Line>();
            List<Grasshopper.Kernel.Types.GH_Line> listof_unit_line = new List<Grasshopper.Kernel.Types.GH_Line>();

            /*if (!(DA.GetDataTree(0, out listofpoint))) { return; }
            if (listofpoint == null && listofpoint.DataCount == 0) { return; }*/

            if (!(DA.GetDataList(0, listofline))) { return; }
            if (listofline == null && listofline.Count == 0) { return; }

            if (toggle_switch)
            {

                System.Windows.Forms.SaveFileDialog Saveprompt = new System.Windows.Forms.SaveFileDialog();
                Saveprompt.Filter = "python script (*.py)|*.py|All files (*.*)|*.*";
                Saveprompt.ShowDialog();

                if (Saveprompt.FileName != "")
                {
                    System.IO.FileStream python_file = (System.IO.FileStream)Saveprompt.OpenFile();


                    List<string> var = new List<string>();

                    var.Add("import rhinoscriptsyntax as rs");

                    string path_file = python_file.Name;
                    python_file.Close();
                    int counter = 0;

                    var.Add("pt=[]");
                    var.Add("lines=[]");

                    for (int i = 0; i < listofline.Count; i++)
                    {
                        Vector3d first_point = new Vector3d(listofline[i].Value.From);
                        Vector3d end_point = new Vector3d(listofline[i].Value.To);

                        if (first_point.Length > 0) { first_point.Unitize(); }
                        if (end_point.Length > 0) { end_point.Unitize(); }

                        var.Add(write_point(first_point));
                        counter++;
                        var.Add(write_point(end_point));
                        counter++;
                        var.Add(write_line(counter));

                        listof_unit_line.Add(new Grasshopper.Kernel.Types.GH_Line());

                    }


                    System.IO.File.WriteAllLines(path_file, var);
                }
                toggle_switch = false;
            }
        }

        private string write_point(Vector3d point)
        {
            return "pt.append([" + point.X + "," + point.Y + "," + point.Z + "])";
        }

        private string write_line(int counter)
        {
            return "lines.append(rs.AddLine(pt[" + (counter - 2) + "],pt[" + (counter - 1) + "]))";
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{1da5aa41-1d7a-4bb7-8812-256b18a3ac95}"); }
        }
    }


    public class GUI : Grasshopper.Kernel.Attributes.GH_ComponentAttributes// change inheritence if want to modify UI
    {

        public GUI(ConvertCell2Python owner)
            : base(owner)
        {
        }

        public override void SetupTooltip(PointF point, GH_TooltipDisplayEventArgs e)
        {
            base.SetupTooltip(point, e);
            e.Description = "Double click to save python file";
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ConvertCell2Python solve = Owner as ConvertCell2Python;

                solve.toggle_switch = true;
                solve.ExpireSolution(true);

                return Grasshopper.GUI.Canvas.GH_ObjectResponse.Handled;

            }

            return Grasshopper.GUI.Canvas.GH_ObjectResponse.Ignore;
        }


    }

}