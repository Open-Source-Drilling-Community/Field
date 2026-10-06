using OSDC.DotnetLibraries.General.ResourceClassification;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Field.Model;

/// <summary>FieldFeatureAssignment contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.FeatureAssignment)] public class FieldFeatureAssignment : FeatureAssignment
{
}
