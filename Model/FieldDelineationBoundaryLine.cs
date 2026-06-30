using OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Field.Model
{
    public class FieldDelineationBoundaryLine
    {
        /// <summary>
        /// stable identifier for the calculated boundary line
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// true if the boundary line represents the interior side of a closed input line
        /// </summary>
        public bool IsInteriorBoundary { get; set; }

        /// <summary>
        /// true if this calculated line is closed
        /// </summary>
        public bool IsClosed { get; set; }

        /// <summary>
        /// calculated boundary line points in SI and WGS84 references
        /// </summary>
        public List<Point3DGlobalCoordinates>? Points { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public FieldDelineationBoundaryLine() : base()
        {
        }
    }
}
