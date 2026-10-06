using OSDC.DotnetLibraries.General.Math;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Field.Model
{
    [Semantic(Concepts.FieldDelineationLine)]
    public class FieldDelineationLine
    {
        /// <summary>
        /// stable identifier for the delineation line
        /// </summary>
        [Semantic(Concepts.ResourceIdentifier)] public Guid ID { get; set; }

        /// <summary>
        /// reference to the standalone delineation line type
        /// </summary>
        [Semantic(Concepts.ResourceIdentifier)] public Guid? DelineationLineTypeID { get; set; }

        /// <summary>
        /// user-defined name of the delineation line
        /// </summary>
        [Semantic(Concepts.ResourceName)] public string? Name { get; set; }

        /// <summary>
        /// user-defined description of the delineation line
        /// </summary>
        [Semantic(Concepts.ResourceDescription)] public string? Description { get; set; }

        /// <summary>
        /// margin distance in SI units. The semantic physical quantity is LengthStandard.
        /// </summary>
        [Semantic(Concepts.DelineationMargin)] public double? Margin { get; set; }

        /// <summary>
        /// optional top depth in SI units and WGS84 depth reference. The semantic physical quantity is DepthDrilling.
        /// </summary>
        [Semantic(Concepts.EllipsoidalDepth, Role = Concepts.TopDepthBoundary, Reference = Concepts.Wgs84)] public double? TopDepth { get; set; }

        /// <summary>
        /// optional bottom depth in SI units and WGS84 depth reference. The semantic physical quantity is DepthDrilling.
        /// </summary>
        [Semantic(Concepts.EllipsoidalDepth, Role = Concepts.BottomDepthBoundary, Reference = Concepts.Wgs84)] public double? BottomDepth { get; set; }

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
