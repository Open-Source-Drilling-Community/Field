using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.Field.Model;

/// <summary>FieldFeatureAssignment contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.FeatureAssignment)]
public class FieldFeatureAssignment : FeatureAssignment
{
}
