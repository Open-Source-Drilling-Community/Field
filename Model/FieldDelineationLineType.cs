using System;
using OSDC.DotnetLibraries.General.DataManagement;

namespace NORCE.Drilling.Field.Model
{
    public class FieldDelineationLineType
    {
        /// <summary>
        /// a MetaInfo for the delineation line type
        /// </summary>
        public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// user-defined name of the delineation line type
        /// </summary>
        public string? Name { get; set; }

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
        public FieldDelineationLineType() : base()
        {
        }
    }
}
