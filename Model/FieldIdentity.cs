using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.Field.Model;

/// <summary>FieldIdentity contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.IdentityDefinition)]
public class FieldIdentity : IdentityDefinition
{
}
