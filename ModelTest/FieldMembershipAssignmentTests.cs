using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model;

namespace NORCE.Drilling.Field.ModelTest;

public class FieldMembershipAssignmentTests
{
    [Test]
    public void Default_State_Is_Empty()
    {
        var data = new FieldMembershipAssignment();

        Assert.That(data.ID, Is.EqualTo(Guid.Empty));
        Assert.That(data.MembershipCategoryID, Is.Null);
        Assert.That(data.MembershipOptionID, Is.Null);
        Assert.That(data.FromDate, Is.Null);
        Assert.That(data.ToDate, Is.Null);
    }

    [Test]
    public void Json_Roundtrip_Preserves_Simple_Values()
    {
        var data = new FieldMembershipAssignment
        {
            ID = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            MembershipCategoryID = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            MembershipOptionID = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            FromDate = DateTimeOffset.Parse("2020-01-01T00:00:00Z"),
            ToDate = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var json = JsonSerializer.Serialize(data, options);
        var clone = JsonSerializer.Deserialize<FieldMembershipAssignment>(json, options);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.ID, Is.EqualTo(data.ID));
        Assert.That(clone.MembershipCategoryID, Is.EqualTo(data.MembershipCategoryID));
        Assert.That(clone.MembershipOptionID, Is.EqualTo(data.MembershipOptionID));
        Assert.That(clone.FromDate, Is.EqualTo(data.FromDate));
        Assert.That(clone.ToDate, Is.EqualTo(data.ToDate));
    }
}
