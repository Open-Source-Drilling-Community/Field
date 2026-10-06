using OSDC.DotnetLibraries.General.ResourceClassification;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Field.Model;

/// <summary>FieldMembershipCategory contract using the shared resource classification implementation.</summary>
[Semantic(Concepts.MembershipCategory)] public class FieldMembershipCategory : MembershipCategory<FieldMembershipOption>
{
}
