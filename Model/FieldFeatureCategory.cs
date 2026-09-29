using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.Field.Model;

/// <summary>FieldFeatureCategory contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.FeatureCategory)]
public class FieldFeatureCategory : FeatureCategory<FieldFeatureOption>
{
}
