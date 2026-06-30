using System;

namespace NORCE.Drilling.Field.Model
{
    public class FieldMembershipAssignment
    {
        /// <summary>
        /// stable identifier for the assignment
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// the selected field membership category
        /// </summary>
        public Guid? FieldMembershipCategoryID { get; set; }

        /// <summary>
        /// the selected field membership option
        /// </summary>
        public Guid? FieldMembershipOptionID { get; set; }

        /// <summary>
        /// first date for which the assignment is valid
        /// </summary>
        public DateTimeOffset? FromDate { get; set; }

        /// <summary>
        /// last date for which the assignment is valid
        /// </summary>
        public DateTimeOffset? ToDate { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public FieldMembershipAssignment() : base()
        {
        }
    }
}
