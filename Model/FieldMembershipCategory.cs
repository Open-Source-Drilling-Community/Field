using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.Field.Model;

/// <summary>FieldMembershipCategory contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.MembershipCategory)]
public class FieldMembershipCategory : MembershipCategory<FieldMembershipOption>
{
}
