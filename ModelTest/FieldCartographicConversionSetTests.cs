using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model; // FieldCartographicConversionSet is declared in namespace Model

namespace NORCE.Drilling.Field.ModelTest;

public class FieldCartographicConversionSetTests
{
    [Test]
    public void Default_State_Is_Null()
    {
        var data = new FieldCartographicConversionSet();

        Assert.That(data.MetaInfo, Is.Null);
        Assert.That(data.Name, Is.Null);
        Assert.That(data.Description, Is.Null);
        Assert.That(data.CreationDate, Is.Null);
        Assert.That(data.LastModificationDate, Is.Null);
        Assert.That(data.FieldID, Is.Null);
        Assert.That(data.CartographicCoordinateList, Is.Null);
    }

    [Test]
    public void Property_Set_Get_Works_For_Simple_Values()
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        var data = new FieldCartographicConversionSet
        {
            Name = "Set A",
            Description = "Conversion set",
            CreationDate = now,
            LastModificationDate = now,
            FieldID = id
        };

        Assert.That(data.Name, Is.EqualTo("Set A"));
        Assert.That(data.Description, Is.EqualTo("Conversion set"));
        Assert.That(data.CreationDate, Is.EqualTo(now));
        Assert.That(data.LastModificationDate, Is.EqualTo(now));
        Assert.That(data.FieldID, Is.EqualTo(id));
    }

    [Test]
    public void Json_Roundtrip_Preserves_Simple_Values()
    {
        var id = Guid.Parse("6b7a9a1d-1b26-4e3b-9d0b-7b8fa2f1c001");
        var data = new FieldCartographicConversionSet
        {
            Name = "Roundtrip",
            Description = "Check",
            FieldID = id
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var json = JsonSerializer.Serialize(data, options);
        var clone = JsonSerializer.Deserialize<FieldCartographicConversionSet>(json, options);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.Name, Is.EqualTo(data.Name));
        Assert.That(clone.Description, Is.EqualTo(data.Description));
        Assert.That(clone.FieldID, Is.EqualTo(data.FieldID));
    }
}

