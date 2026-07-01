using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model;
using OSDC.DotnetLibraries.General.DataManagement;

namespace NORCE.Drilling.Field.ModelTest;

public class FieldFeatureCategoryTests
{
    [Test]
    public void Default_State_Is_Null_Or_Default()
    {
        var category = new FieldFeatureCategory();

        Assert.That(category.MetaInfo, Is.Null);
        Assert.That(category.Name, Is.Null);
        Assert.That(category.IsExclusive, Is.False);
        Assert.That(category.HasValidityPeriod, Is.False);
        Assert.That(category.Options, Is.Null);
        Assert.That(category.CreationDate, Is.Null);
        Assert.That(category.LastModificationDate, Is.Null);
    }

    [Test]
    public void Json_Roundtrip_Preserves_Values()
    {
        Guid id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid optionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        DateTimeOffset now = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
        FieldFeatureCategory category = new()
        {
            MetaInfo = new MetaInfo { ID = id },
            Name = "production role",
            IsExclusive = true,
            HasValidityPeriod = true,
            Options =
            [
                new FieldFeatureOption { ID = optionId, Name = "producer" }
            ],
            CreationDate = now,
            LastModificationDate = now
        };

        string json = JsonSerializer.Serialize(category, new JsonSerializerOptions { PropertyNamingPolicy = null });
        FieldFeatureCategory? clone = JsonSerializer.Deserialize<FieldFeatureCategory>(json, new JsonSerializerOptions { PropertyNamingPolicy = null });

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.MetaInfo?.ID, Is.EqualTo(id));
        Assert.That(clone.Name, Is.EqualTo(category.Name));
        Assert.That(clone.IsExclusive, Is.True);
        Assert.That(clone.HasValidityPeriod, Is.True);
        Assert.That(clone.Options, Has.Count.EqualTo(1));
        Assert.That(clone.Options![0].ID, Is.EqualTo(optionId));
        Assert.That(clone.Options[0].Name, Is.EqualTo("producer"));
        Assert.That(clone.CreationDate, Is.EqualTo(category.CreationDate));
        Assert.That(clone.LastModificationDate, Is.EqualTo(category.LastModificationDate));
    }
}
