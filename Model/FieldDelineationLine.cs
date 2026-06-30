using OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Field.Model
{
    public class FieldDelineationLine
    {
        /// <summary>
        /// stable identifier for the delineation line
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// reference to the standalone delineation line type
        /// </summary>
        public Guid? DelineationLineTypeID { get; set; }

        /// <summary>
        /// legacy user-defined line type name kept for backward-compatible JSON imports
        /// </summary>
        public string? LineType { get; set; }

        /// <summary>
        /// user-defined name of the delineation line
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// user-defined description of the delineation line
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// margin distance in SI units. The semantic physical quantity is LengthStandard.
        /// </summary>
        public double? Margin { get; set; }

        /// <summary>
        /// optional top depth in SI units and WGS84 depth reference. The semantic physical quantity is DepthDrilling.
        /// </summary>
        public double? TopDepth { get; set; }

        /// <summary>
        /// optional bottom depth in SI units and WGS84 depth reference. The semantic physical quantity is DepthDrilling.
        /// </summary>
        public double? BottomDepth { get; set; }

        /// <summary>
        /// original delineation line points in SI and WGS84 references
        /// </summary>
        public List<Point3DGlobalCoordinates>? Points { get; set; }

        /// <summary>
        /// service-calculated boundary lines derived from Points and Margin
        /// </summary>
        public List<FieldDelineationBoundaryLine>? CalculatedBoundaryLines { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public FieldDelineationLine() : base()
        {
        }
    }
}
