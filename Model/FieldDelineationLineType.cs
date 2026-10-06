using System;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Field.Model
{
    [Semantic(Concepts.DelineationLineType)]
    public class FieldDelineationLineType
    {
        /// <summary>
        /// a MetaInfo for the delineation line type
        /// </summary>
        [Semantic(Concepts.ResourceMetadata)] public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// user-defined name of the delineation line type
        /// </summary>
        [Semantic(Concepts.ResourceName)] public string? Name { get; set; }

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
        public FieldDelineationLineType() : base()
        {
        }
    }
}
