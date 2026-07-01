using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Field.Model
{
    public class FieldFeatureOption : IFeatureOption
    {
        /// <summary>
        /// stable identifier for the option inside its category
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// user-defined name of the option
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public FieldFeatureOption() : base()
        {
        }
    }
}
