using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Field.Model
{
    /// <summary>
    /// Light weight version of a Field.
    /// Used to avoid transferring complete Field data when only contextual information is needed.
    /// </summary>
    public class FieldLight
    {
        /// <summary>
        /// a MetaInfo for the FieldLight
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
