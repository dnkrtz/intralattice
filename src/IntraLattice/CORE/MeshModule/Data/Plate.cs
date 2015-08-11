using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntraLattice.CORE.MeshModule.Data
{
    public class Plate
    {
        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="SetNodeIndex">Index of parent node.</param>
        /// <param name="SetNormal">Normal vector to plate, oriented in the direction of offset.</param>
        public Plate(int SetNodeIndex, Vector3d SetNormal)
        {
            this.NodeIndex = SetNodeIndex;
            this.Normal = SetNormal;
            this.Vtc = new List<Point3d>();
        }

        /// <summary>
        /// Offset of plate from it's parent node.
        /// </summary>
        public double Offset { get; set; }
        /// <summary>
        /// Normal vector to plate, oriented in the direction of offset.
        /// </summary>
        public Vector3d Normal { get; set; }
        /// <summary>
        /// Plate vertices (Vtc[0] should be the centerpoint of the plate)
        /// </summary>
        public List<Point3d> Vtc { get; set; }
        /// <summary>
        /// Index of the parent node.
        /// </summary>
        public int NodeIndex { get; set; }
    }
}
