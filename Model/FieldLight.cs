using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using System;

namespace OSDC.Drilling.Field.Model
{
    /// <summary>
    /// Light weight version of a Field.
    /// Used to avoid transferring complete Field data when only contextual information is needed.
    /// </summary>
    [Semantic(Concepts.Field)]
    public class FieldLight
    {
        /// <summary>
        /// a MetaInfo for the FieldLight
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
        /// default constructor required for JSON serialization
        /// </summary>
        public FieldLight() : base()
        {
        }

        /// <summary>
        /// base constructor
        /// </summary>
        public FieldLight(MetaInfo? metaInfo, string? name, string? description, DateTimeOffset? creationDate, DateTimeOffset? lastModificationDate)
        {
            MetaInfo = metaInfo;
            Name = name;
            Description = description;
            CreationDate = creationDate;
            LastModificationDate = lastModificationDate;
        }
    }
}
