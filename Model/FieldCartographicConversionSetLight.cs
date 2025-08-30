using System;
using System.Collections.Generic;
using NORCE.Drilling.Field.ModelShared;

namespace NORCE.Drilling.Field.Model
{
    /// <summary>
    /// A light version of FieldCartographicConversionSet without methods and data.
    /// </summary>
    public class FieldCartographicConversionSetLight
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
        /// the name of the reference field
        /// </summary>
        public string? FieldName { get; set; }
        /// <summary>
        /// the description of the reference field
        /// </summary>
        public string? FieldDescription { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public FieldCartographicConversionSetLight() : base()
        {
        }
        public FieldCartographicConversionSetLight(OSDC.DotnetLibraries.General.DataManagement.MetaInfo? metaInfo, string? name, string? descr, DateTimeOffset? creationDate, DateTimeOffset? modifDate, string? fieldName, string? fieldDescr)
        {
            MetaInfo = metaInfo;
            Name = name;
            Description = descr;
            CreationDate = creationDate;
            LastModificationDate = modifDate;
            FieldName = fieldName;
            FieldDescription = fieldDescr;
        }
    }
}
