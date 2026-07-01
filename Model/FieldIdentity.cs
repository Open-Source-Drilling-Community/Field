using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Field.Model
{
    public class FieldIdentity : IIdentity
    {
        /// <summary>
        /// a MetaInfo for the FieldIdentity
        /// </summary>
        public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// symbolic name of the identity
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
        public FieldIdentity() : base()
        {
        }
    }
}
