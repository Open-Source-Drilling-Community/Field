using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Field.Model
{
    public class FieldIdentityAssignment : IIdentityAssignment
    {
        /// <summary>
        /// unique ID of the assignment
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// reference to the selected FieldIdentity
        /// </summary>
        public Guid? IdentityID { get; set; }

        /// <summary>
        /// field-specific identity value
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public FieldIdentityAssignment() : base()
        {
        }
    }
}
