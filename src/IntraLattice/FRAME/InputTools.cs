using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

namespace IntraLattice
{
    public class InputTools
    {

        // index represents the input position (first input is index == 0)
        public static void TopoSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index)
        {
            //instantiate  new value list
            var vallist = new Grasshopper.Kernel.Special.GH_ValueList();
            vallist.ListMode = Grasshopper.Kernel.Special.GH_ValueListMode.Cycle;
            vallist.CreateAttributes();

            //customise value list position
            float xCoord = (float)Component.Attributes.Pivot.X - 200;
            float yCoord = (float)Component.Attributes.Pivot.Y - index * 50;
            PointF cornerPt = new PointF(xCoord, yCoord);
            vallist.Attributes.Pivot = cornerPt;

            //populate value list with our own data
            vallist.ListItems.Clear();
            var item1 = new Grasshopper.Kernel.Special.GH_ValueListItem("Grid", "0");
            var item2 = new Grasshopper.Kernel.Special.GH_ValueListItem("X", "1");
            var item3 = new Grasshopper.Kernel.Special.GH_ValueListItem("Star", "2");
            var item4 = new Grasshopper.Kernel.Special.GH_ValueListItem("Cross", "3");
            var item5 = new Grasshopper.Kernel.Special.GH_ValueListItem("Cross2", "4");
            vallist.ListItems.Add(item1);
            vallist.ListItems.Add(item2);
            vallist.ListItems.Add(item3);
            vallist.ListItems.Add(item4);
            vallist.ListItems.Add(item5);

            // Until now, the slider is a hypothetical object.
            // This command makes it 'real' and adds it to the canvas.
            GrasshopperDocument.AddObject(vallist, false);

            //Connect the new slider to this component
            Component.Params.Input[index].AddSource(vallist);
            Component.Params.Input[index].CollectData();
        }

        public static void BooleanSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index)
        {
            //instantiate  new value list
            var boollist = new Grasshopper.Kernel.Special.GH_BooleanToggle();
            boollist.CreateAttributes();

            //customise value list position
            float xCoord = (float)Component.Attributes.Pivot.X - 200;
            float yCoord = (float)Component.Attributes.Pivot.Y - index * 50;
            PointF cornerPt = new PointF(xCoord, yCoord);
            boollist.Attributes.Pivot = cornerPt;

            //set value
            boollist.Value = true;

            // Until now, the slider is a hypothetical object.
            // This command makes it 'real' and adds it to the canvas.
            GrasshopperDocument.AddObject(boollist, false);

            //Connect the new slider to this component
            Component.Params.Input[2].AddSource(boollist);
        }

        public static void IntegerSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index)
        {

        }

        public static void FloatSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index)
        {

        }

        public static void SurfaceSelect(ref IGH_Component Component, ref GH_Document GrasshopperDocument, int index)
        {

        }


    }
}