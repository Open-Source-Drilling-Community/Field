using OSDC.DotnetLibraries.General.DataManagement;
using System;

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
        /// default constructor required for JSON serialization
        /// </summary>
        public Field() : base()
        {
        }
    }
}
