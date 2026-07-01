using System;
using System.Text.Json;
using NUnit.Framework;
using NORCE.Drilling.Field.Model;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Math;

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
        Assert.That(field.ReferencePoint, Is.Null);
        Assert.That(field.FieldFeatureAssignments, Is.Null);
        Assert.That(field.DelineationLines, Is.Null);
    }

    [Test]
    public void Property_Set_Get_Works_For_Simple_Values()
    {
        var now = DateTimeOffset.UtcNow;
        var later = now.AddMinutes(5);
        Guid featureCategoryId = Guid.NewGuid();
        Guid featureOptionId = Guid.NewGuid();
        Guid lineTypeId = Guid.NewGuid();

        var field = new Model.Field
        {
            Name = "Test Field",
            Description = "Description",
            CreationDate = now,
            LastModificationDate = later,
            ReferencePoint = new Point3DGlobalCoordinates
            {
                RiemannianNorth = 1234.5,
                RiemannianEast = 6789.0,
                TVD = 0,
                Z = 0
            },
            FieldFeatureAssignments =
            [
                new FieldFeatureAssignment
                {
                    ID = Guid.NewGuid(),
                    FeatureCategoryID = featureCategoryId,
                    FeatureOptionID = featureOptionId,
                    FromDate = now,
                    ToDate = later
                }
            ],
            DelineationLines =
            [
                new FieldDelineationLine
                {
                    ID = Guid.NewGuid(),
                    DelineationLineTypeID = lineTypeId,
                    LineType = "lease line",
                    Name = "North lease line",
                    Margin = 100,
                    TopDepth = 1000,
                    BottomDepth = 2000,
                    Points =
                    [
                        new Point3DGlobalCoordinates { RiemannianNorth = 0, RiemannianEast = 0, TVD = 0 },
                        new Point3DGlobalCoordinates { RiemannianNorth = 1000, RiemannianEast = 0, TVD = 0 }
                    ]
                }
            ]
        };

        Assert.That(field.Name, Is.EqualTo("Test Field"));
        Assert.That(field.Description, Is.EqualTo("Description"));
        Assert.That(field.CreationDate, Is.EqualTo(now));
        Assert.That(field.LastModificationDate, Is.EqualTo(later));
        Assert.That(field.ReferencePoint, Is.Not.Null);
        Assert.That(field.ReferencePoint!.RiemannianNorth, Is.EqualTo(1234.5));
        Assert.That(field.ReferencePoint.RiemannianEast, Is.EqualTo(6789.0));
        Assert.That(field.ReferencePoint.Latitude, Is.Not.Null);
        Assert.That(field.ReferencePoint.Longitude, Is.Not.Null);
        Assert.That(field.FieldFeatureAssignments, Has.Count.EqualTo(1));
        Assert.That(field.FieldFeatureAssignments![0].FeatureCategoryID, Is.EqualTo(featureCategoryId));
        Assert.That(field.FieldFeatureAssignments[0].FeatureOptionID, Is.EqualTo(featureOptionId));
        Assert.That(field.DelineationLines, Has.Count.EqualTo(1));
        Assert.That(field.DelineationLines![0].Name, Is.EqualTo("North lease line"));
        Assert.That(field.DelineationLines[0].DelineationLineTypeID, Is.EqualTo(lineTypeId));
        Assert.That(Numeric.EQ(field.DelineationLines[0].TopDepth, 1000), Is.True);
        Assert.That(Numeric.EQ(field.DelineationLines[0].BottomDepth, 2000), Is.True);
        Assert.That(field.DelineationLines[0].Points, Has.Count.EqualTo(2));
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
            LastModificationDate = now,
            ReferencePoint = new Point3DGlobalCoordinates
            {
                RiemannianNorth = 100,
                RiemannianEast = 200,
                TVD = 0,
                Z = 0
            },
            FieldFeatureAssignments =
            [
                new FieldFeatureAssignment
                {
                    ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    FeatureCategoryID = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    FeatureOptionID = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    FromDate = now,
                    ToDate = null
                }
            ],
            DelineationLines =
            [
                new FieldDelineationLine
                {
                    ID = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    DelineationLineTypeID = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    LineType = "border line",
                    Name = "Border",
                    Description = "A border line",
                    Margin = 50,
                    TopDepth = 500,
                    BottomDepth = 1500,
                    Points =
                    [
                        new Point3DGlobalCoordinates { RiemannianNorth = 0, RiemannianEast = 0, TVD = 10 },
                        new Point3DGlobalCoordinates { RiemannianNorth = 0, RiemannianEast = 1000, TVD = 10 }
                    ],
                    CalculatedBoundaryLines =
                    [
                        new FieldDelineationBoundaryLine
                        {
                            ID = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                            Points =
                            [
                                new Point3DGlobalCoordinates { RiemannianNorth = 50, RiemannianEast = 0, TVD = 10 },
                                new Point3DGlobalCoordinates { RiemannianNorth = 50, RiemannianEast = 1000, TVD = 10 }
                            ]
                        }
                    ]
                }
            ]
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = null };
        var json = JsonSerializer.Serialize(field, options);
        var clone = JsonSerializer.Deserialize<Model.Field>(json, options);

        Assert.That(clone, Is.Not.Null);
        Assert.That(clone!.Name, Is.EqualTo(field.Name));
        Assert.That(clone.Description, Is.EqualTo(field.Description));
        Assert.That(clone.CreationDate, Is.EqualTo(field.CreationDate));
        Assert.That(clone.LastModificationDate, Is.EqualTo(field.LastModificationDate));
        Assert.That(clone.ReferencePoint, Is.Not.Null);
        Assert.That(field.ReferencePoint, Is.Not.Null);
        Assert.That(clone.ReferencePoint!.RiemannianNorth!.Value, Is.EqualTo(field.ReferencePoint!.RiemannianNorth!.Value).Within(1e-9));
        Assert.That(clone.ReferencePoint.RiemannianEast!.Value, Is.EqualTo(field.ReferencePoint.RiemannianEast!.Value).Within(1e-9));
        Assert.That(clone.ReferencePoint.Latitude!.Value, Is.EqualTo(field.ReferencePoint.Latitude!.Value).Within(1e-12));
        Assert.That(clone.ReferencePoint.Longitude!.Value, Is.EqualTo(field.ReferencePoint.Longitude!.Value).Within(1e-12));
        Assert.That(clone.FieldFeatureAssignments, Has.Count.EqualTo(1));
        Assert.That(clone.FieldFeatureAssignments![0].ID, Is.EqualTo(field.FieldFeatureAssignments![0].ID));
        Assert.That(clone.FieldFeatureAssignments[0].FeatureCategoryID, Is.EqualTo(field.FieldFeatureAssignments[0].FeatureCategoryID));
        Assert.That(clone.FieldFeatureAssignments[0].FeatureOptionID, Is.EqualTo(field.FieldFeatureAssignments[0].FeatureOptionID));
        Assert.That(clone.FieldFeatureAssignments[0].FromDate, Is.EqualTo(field.FieldFeatureAssignments[0].FromDate));
        Assert.That(clone.DelineationLines, Has.Count.EqualTo(1));
        Assert.That(clone.DelineationLines![0].ID, Is.EqualTo(field.DelineationLines![0].ID));
        Assert.That(clone.DelineationLines[0].DelineationLineTypeID, Is.EqualTo(field.DelineationLines[0].DelineationLineTypeID));
        Assert.That(Numeric.EQ(clone.DelineationLines[0].TopDepth, field.DelineationLines[0].TopDepth), Is.True);
        Assert.That(Numeric.EQ(clone.DelineationLines[0].BottomDepth, field.DelineationLines[0].BottomDepth), Is.True);
        Assert.That(clone.DelineationLines[0].Points, Has.Count.EqualTo(2));
        Assert.That(clone.DelineationLines[0].CalculatedBoundaryLines, Has.Count.EqualTo(1));
    }
}

