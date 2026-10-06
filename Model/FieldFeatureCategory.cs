using OSDC.DotnetLibraries.General.ResourceClassification;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Field.Model;

/// <summary>FieldFeatureCategory contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.FeatureCategory)] public class FieldFeatureCategory : FeatureCategory<FieldFeatureOption>
{
}
