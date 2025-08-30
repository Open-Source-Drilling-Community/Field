using System;
using System.Collections.Generic;
using NORCE.Drilling.Field.ModelShared;

namespace NORCE.Drilling.Field.Model
{
    /// <summary>
    /// A field cartographic conversion set is a series of CartographicCoordinate.
    /// The cartographic data referred to the cartographic projection of the field are converted to the target geodetic datum and WGS84 or vice versa.
    /// The grid convergence at the local is also calculated in the geodetic datum and in the WGS84 datum.
    /// The octree code for the WGS84 geodetic position can also calculated at the requested level of details.
    /// it is also possible to pass the octree code (in the WGS84 datum), and then the geodetic coordinates and cartographic coordinates are calculated
    /// </summary>
    public class FieldCartographicConversionSet
    {
        /// <summary>
        /// a MetaInfo for the CartographicConversionSet
        /// </summary>
        public OSDC.DotnetLibraries.General.DataManagement.MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// name of the data
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// a description of the data
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// the date when the data was created
        /// </summary>
        public DateTimeOffset? CreationDate { get; set; }

        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        public DateTimeOffset? LastModificationDate { get; set; }

        /// <summary>
        /// the ID of the Field
        /// </summary>
        public Guid? FieldID { get; set; } = null;

        /// <summary>
        /// an input list of CartographicCoordinate
        /// </summary>
        public List<CartographicCoordinate>? CartographicCoordinateList { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public FieldCartographicConversionSet() : base()
        {
        }

    }
}
