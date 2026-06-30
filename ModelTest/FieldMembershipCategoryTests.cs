using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model;
using OSDC.DotnetLibraries.General.DataManagement;

namespace NORCE.Drilling.Field.ModelTest;

public class FieldMembershipCategoryTests
{
    [Test]
    public void Default_State_Is_Empty()
    {
        var data = new FieldMembershipCategory();

        Assert.That(data.MetaInfo, Is.Null);
        Assert.That(data.Name, Is.Null);
        Assert.That(data.IsExclusive, Is.False);
        Assert.That(data.HasValidityPeriod, Is.False);
        Assert.That(data.Options, Is.Null);
        Assert.That(data.CreationDate, Is.Null);
        Assert.That(data.LastModificationDate, Is.Null);
    }

    [Test]
    public void Json_Roundtrip_Preserves_Simple_Values()
    {
        var now = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
        var optionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var data = new FieldMembershipCategory
        {
            MetaInfo = new MetaInfo { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
            Name = "Basin",
            IsExclusive = false,
            HasValidityPeriod = true,
            Options = [new FieldMembershipOption { ID = optionId, Name = "North Sea Basin" }],
            CreationDate = now,
            LastModificationDate = now
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var json = JsonSerializer.Serialize(data, options);
        var clone = JsonSerializer.Deserialize<FieldMembershipCategory>(json, options);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.MetaInfo!.ID, Is.EqualTo(data.MetaInfo.ID));
        Assert.That(clone.Name, Is.EqualTo(data.Name));
        Assert.That(clone.HasValidityPeriod, Is.True);
        Assert.That(clone.Options, Has.Count.EqualTo(1));
        Assert.That(clone.Options![0].ID, Is.EqualTo(optionId));
        Assert.That(clone.Options[0].Name, Is.EqualTo("North Sea Basin"));
    }
}
