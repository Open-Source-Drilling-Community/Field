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
    public class FieldCartographicConversionSet : FieldCartographicConversionSetLight
    {
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
