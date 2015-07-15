using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;


namespace IntraLattice
{
    class TopologySymmetryCheck
    {
        public bool IsSymmetric(Brep unitCell)
        {
            bool checkSymmetry= false;
            var UnitCellProperty =  VolumeMassProperties.Compute(unitCell);
            var centroid = UnitCellProperty.Centroid;



            return checkSymmetry;
        }

    }
}
