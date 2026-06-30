using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model;
using OSDC.DotnetLibraries.General.DataManagement;

namespace NORCE.Drilling.Field.ModelTest;

public class FieldIdentityTests
{
    [Test]
    public void Default_State_Is_Null()
    {
        var data = new FieldIdentity();

        Assert.That(data.MetaInfo, Is.Null);
        Assert.That(data.Name, Is.Null);
        Assert.That(data.CreationDate, Is.Null);
        Assert.That(data.LastModificationDate, Is.Null);
    }

    [Test]
    public void Json_Roundtrip_Preserves_Simple_Values()
    {
        var now = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
        var data = new FieldIdentity
        {
            MetaInfo = new MetaInfo { ID = Guid.Parse("88888888-8888-8888-8888-888888888888") },
            Name = "NPD field ID",
            CreationDate = now,
            LastModificationDate = now
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var json = JsonSerializer.Serialize(data, options);
        var clone = JsonSerializer.Deserialize<FieldIdentity>(json, options);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.MetaInfo!.ID, Is.EqualTo(data.MetaInfo.ID));
        Assert.That(clone.Name, Is.EqualTo(data.Name));
        Assert.That(clone.CreationDate, Is.EqualTo(data.CreationDate));
        Assert.That(clone.LastModificationDate, Is.EqualTo(data.LastModificationDate));
    }
}
