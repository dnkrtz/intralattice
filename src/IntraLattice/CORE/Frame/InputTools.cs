using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

// Summary:     This class contains a set of methods used to automatically generate input menus
// ===============================================================================
// Methods:     TopoSelect (written by Aidan)       - Menu for unit cell topologhy
//              GradientSelect (written by Aidan)   - Menu for thickness gradient expressions
// ===============================================================================
// Issues:      Menus are generated after the component runs succesfully. This means that we cannot automatically generate menus for
//              components which require inputs that are non-default-able.
// ===============================================================================
// Author(s):   Aidan Kurtz (http://aidankurtz.com)

namespace IntraLattice
{
    public class InputTools
    {
        /// <summary>
        /// The 'index' input represents the input index (first input is index 0)
        /// The 'offset' parameter is the vertical offset of the menu, to help with positioning
        /// </summary>
        public static void TopoSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index, float offset)
        {
            //instantiate  new value list
            var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
            vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.Cycle;
            vallist.CreateAttributes();

            //customise value list position
            float xCoord = (float)Component.Attributes.Pivot.X - 200;
            float yCoord = (float)Component.Attributes.Pivot.Y + index * 40 - offset;
            PointF cornerPt = new PointF(xCoord, yCoord);
            vallist.Attributes.Pivot = cornerPt;

            //populate value list with our own data
            vallist.ListItems.Clear();
            var items = new List<Grasshopper.Kernel.Special.GH_ValueListItem>();
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Grid", "0"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("X", "1"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Star", "2"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Cross", "3"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Cross2", "4"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Vintiles", "5"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Octahedral", "6"));

            vallist.ListItems.AddRange(items);

            // Until now, the slider is a hypothetical object.
            // This command makes it 'real' and adds it to the canvas.
            GrasshopperDocument.AddObject(vallist, false);

            //Connect the new slider to this component
            Component.Params.Input[index].AddSource(vallist);
            Component.Params.Input[index].CollectData();
        }

        // index represents the input position (first input is index == 0)
        public static void GradientSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index, float offset)
        {
            //instantiate  new value list
            var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
            vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.DropDown;
            vallist.CreateAttributes();

            //customise value list position
            float xCoord = (float)Component.Attributes.Pivot.X - 200;
            float yCoord = (float)Component.Attributes.Pivot.Y + index * 40 - offset;
            PointF cornerPt = new PointF(xCoord, yCoord);
            vallist.Attributes.Pivot = cornerPt;

            //populate value list with our own data
            vallist.ListItems.Clear();
            var items = new List<Grasshopper.Kernel.Special.GH_ValueListItem>();
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Linear (X)", "0"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Linear (Y)", "1"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Linear (Z)", "2"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Centered (X)", "3"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Centered (Y)", "4"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Centered (Z)", "5"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Cylindrical (X)", "6"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Cylindrical (Y)", "7"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Cylindrical (Z)", "8"));
            items.Add(new Grasshopper.Kernel.Special.GH_ValueListItem("Spherical", "9"));

            vallist.ListItems.AddRange(items);

            // Until now, the slider is a hypothetical object.
            // This command makes it 'real' and adds it to the canvas.
            GrasshopperDocument.AddObject(vallist, false);

            //Connect the new slider to this component
            Component.Params.Input[index].AddSource(vallist);
            Component.Params.Input[index].CollectData();
        }

        /*public static void BooleanSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index, float offset)
        {
            //instantiate  new value list
            var boollist = new Grasshopper.Kernel.Special.GH_BooleanToggle();
            boollist.CreateAttributes();

            //customise value list position
            float xCoord = (float)Component.Attributes.Pivot.X - 200;
            float yCoord = (float)Component.Attributes.Pivot.Y + index*40 - offset;
            PointF cornerPt = new PointF(xCoord, yCoord);
            boollist.Attributes.Pivot = cornerPt;
            
            // Until now, the slider is a hypothetical object.
            // This command makes it 'real' and adds it to the canvas.
            GrasshopperDocument.AddObject(boollist, false);
            //Connect the new slider to this component
            Component.Params.Input[index].AddSource(boollist);
            Component.Params.Input[index].CollectData();
            // Little hack, required because of how booleantoggle is rendered
            boollist.ExpireSolution(true);
        }

        public static void NumberSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index, float offset, int min, int max, bool isFloat)
        {
            //instantiate  new value list
            var numberSlider = new Grasshopper.Kernel.Special.GH_NumberSlider();
            numberSlider.Slider.Minimum = min;
            numberSlider.Slider.Maximum = max;
            if (isFloat)
                numberSlider.Slider.Type = Grasshopper.GUI.Base.GH_SliderAccuracy.Integer;
            else
                numberSlider.Slider.Type = Grasshopper.GUI.Base.GH_SliderAccuracy.Float;
            numberSlider.CreateAttributes();

            //customise value list position
            float xCoord = (float)Component.Attributes.Pivot.X - 200;
            float yCoord = (float)Component.Attributes.Pivot.Y + index * 40;
            PointF cornerPt = new PointF(xCoord, yCoord);
            numberSlider.Attributes.Pivot = cornerPt;

            // Until now, the slider is a hypothetical object.
            // This command makes it 'real' and adds it to the canvas.
            GrasshopperDocument.AddObject(numberSlider, false);

            //Connect the new slider to this component
            Component.Params.Input[index].AddSource(numberSlider);
            Component.Params.Input[index].CollectData();
        }*/


    }
}