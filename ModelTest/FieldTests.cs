using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model;

namespace NORCE.Drilling.Field.ModelTest;

public class FieldTests
{
    [Test]
    public void Default_State_Is_Null()
    {
        var field = new Model.Field();

        Assert.That(field.MetaInfo, Is.Null);
        Assert.That(field.Name, Is.Null);
        Assert.That(field.Description, Is.Null);
        Assert.That(field.CreationDate, Is.Null);
        Assert.That(field.LastModificationDate, Is.Null);
        Assert.That(field.CartographicProjectionID, Is.Null);
    }

    [Test]
    public void Property_Set_Get_Works_For_Simple_Values()
    {
        var now = DateTimeOffset.UtcNow;
        var later = now.AddMinutes(5);

        var field = new Model.Field
        {
            Name = "Test Field",
            Description = "Description",
            CreationDate = now,
            LastModificationDate = later
        };

        Assert.That(field.Name, Is.EqualTo("Test Field"));
        Assert.That(field.Description, Is.EqualTo("Description"));
        Assert.That(field.CreationDate, Is.EqualTo(now));
        Assert.That(field.LastModificationDate, Is.EqualTo(later));
    }

    [Test]
    public void Json_Roundtrip_Preserves_Simple_Values()
    {
        var now = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
        var field = new Model.Field
        {
            Name = "Roundtrip",
            Description = "Check",
            CreationDate = now,
            LastModificationDate = now
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var json = JsonSerializer.Serialize(field, options);
        var clone = JsonSerializer.Deserialize<Model.Field>(json, options);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.Name, Is.EqualTo(field.Name));
        Assert.That(clone.Description, Is.EqualTo(field.Description));
        Assert.That(clone.CreationDate, Is.EqualTo(field.CreationDate));
        Assert.That(clone.LastModificationDate, Is.EqualTo(field.LastModificationDate));
    }
}

