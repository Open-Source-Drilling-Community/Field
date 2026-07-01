using System;
using OSDC.DotnetLibraries.General.DataManagement;

namespace NORCE.Drilling.Field.Model
{
    public class FieldFeatureAssignment : IFeatureAssignment
    {
        /// <summary>
        /// stable identifier for the assignment
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// the selected field feature category
        /// </summary>
        public Guid? FeatureCategoryID { get; set; }

        /// <summary>
        /// the selected field feature option
        /// </summary>
        public Guid? FeatureOptionID { get; set; }

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
        public FieldFeatureAssignment() : base()
        {
        }
    }
}
