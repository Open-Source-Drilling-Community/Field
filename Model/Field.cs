using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Field.Model
{
    public class Field
    {
        /// <summary>
        /// a MetaInfo for the Field
        /// </summary>
        public MetaInfo? MetaInfo { get; set; }

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
        /// a reference to the CartographicProjection
        /// </summary>
        public Guid? CartographicProjectionID { get; set; }

        /// <summary>
        /// optional reference point for the field in SI and WGS84 references
        /// </summary>
        public Point3DGlobalCoordinates? ReferencePoint { get; set; }

        /// <summary>
        /// the selected field feature assignments associated with the field
        /// </summary>
        public List<FieldFeatureAssignment>? FieldFeatureAssignments { get; set; }

        /// <summary>
        /// the selected identities associated with the field
        /// </summary>
        public List<FieldIdentityAssignment>? FieldIdentityAssignments { get; set; }

        /// <summary>
        /// the selected field membership assignments associated with the field
        /// </summary>
        public List<FieldMembershipAssignment>? FieldMembershipAssignments { get; set; }

        /// <summary>
        /// the list of user-defined delineation lines associated with the field
        /// </summary>
        public List<FieldDelineationLine>? DelineationLines { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public Field() : base()
        {
        }
    }
}
