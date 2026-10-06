using OSDC.DotnetLibraries.General.ResourceClassification;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Field.Model;

/// <summary>FieldIdentity contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.IdentityDefinition)] public class FieldIdentity : IdentityDefinition
{
}
