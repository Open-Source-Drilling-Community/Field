using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model;
using OSDC.DotnetLibraries.General.DataManagement;

namespace NORCE.Drilling.Field.ModelTest;

public class FieldDelineationLineTypeTests
{
    [Test]
    public void Default_State_Is_Null()
    {
        var data = new FieldDelineationLineType();

        Assert.That(data.MetaInfo, Is.Null);
        Assert.That(data.Name, Is.Null);
        Assert.That(data.CreationDate, Is.Null);
        Assert.That(data.LastModificationDate, Is.Null);
    }

    [Test]
    public void Json_Roundtrip_Preserves_Simple_Values()
    {
        var now = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
        var data = new FieldDelineationLineType
        {
            MetaInfo = new MetaInfo { ID = Guid.Parse("77777777-7777-7777-7777-777777777777") },
            Name = "lease line",
            CreationDate = now,
            LastModificationDate = now
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var json = JsonSerializer.Serialize(data, options);
        var clone = JsonSerializer.Deserialize<FieldDelineationLineType>(json, options);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.MetaInfo!.ID, Is.EqualTo(data.MetaInfo.ID));
        Assert.That(clone.Name, Is.EqualTo(data.Name));
        Assert.That(clone.CreationDate, Is.EqualTo(data.CreationDate));
        Assert.That(clone.LastModificationDate, Is.EqualTo(data.LastModificationDate));
    }
}
