using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model;

namespace NORCE.Drilling.Field.ModelTest;

public class FieldIdentityAssignmentTests
{
    [Test]
    public void Default_State_Is_Empty()
    {
        var data = new FieldIdentityAssignment();

        Assert.That(data.ID, Is.EqualTo(Guid.Empty));
        Assert.That(data.IdentityID, Is.Null);
        Assert.That(data.Value, Is.Null);
    }

    [Test]
    public void Json_Roundtrip_Preserves_Simple_Values()
    {
        var data = new FieldIdentityAssignment
        {
            ID = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            IdentityID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Value = "12345"
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var json = JsonSerializer.Serialize(data, options);
        var clone = JsonSerializer.Deserialize<FieldIdentityAssignment>(json, options);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.ID, Is.EqualTo(data.ID));
        Assert.That(clone.IdentityID, Is.EqualTo(data.IdentityID));
        Assert.That(clone.Value, Is.EqualTo(data.Value));
    }
}
