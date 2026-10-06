using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Field.Model
{
    [Semantic(Concepts.Field)]
    public class Field
    {
        /// <summary>
        /// a MetaInfo for the Field
        /// </summary>
        [Semantic(Concepts.ResourceMetadata)] public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// name of the data
        /// </summary>
        [Semantic(Concepts.ResourceName)] public string? Name { get; set; }

        /// <summary>
        /// a description of the data
        /// </summary>
        [Semantic(Concepts.ResourceDescription)] public string? Description { get; set; }

        /// <summary>
        /// the date when the data was created
        /// </summary>
        [Semantic(Concepts.Instant, Role = Concepts.CreationTime, Reference = Concepts.Utc)] public DateTimeOffset? CreationDate { get; set; }

        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        [Semantic(Concepts.Instant, Role = Concepts.LastModificationTime, Reference = Concepts.Utc)] public DateTimeOffset? LastModificationDate { get; set; }
        
        /// <summary>
        /// a reference to an EarthCartographicProjection projection definition
        /// </summary>
        [Semantic(Concepts.ResourceIdentifier)] public Guid? ProjectionDefinitionID { get; set; }

        /// <summary>
        /// optional reference point for the field in SI and WGS84 references
        /// </summary>
        [Semantic(Concepts.Position, Role = Concepts.ReferenceLocation, Reference = Concepts.Wgs84)] public Point3DGlobalCoordinates? ReferencePoint { get; set; }

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
