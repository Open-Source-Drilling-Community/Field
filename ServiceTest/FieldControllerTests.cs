using System.Net.Http.Headers;
using OSDC.Drilling.Field.ModelShared;
using FieldModel = OSDC.Drilling.Field.ModelShared.Field;

namespace OSDC.Drilling.Field.ServiceTest
{
    public class FieldControllerTests
    {
        private static string host = "https://localhost:5001/";
        private static HttpClient httpClient;
        private static Client api;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(host + "Field/api/")
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            api = new Client(httpClient.BaseAddress.ToString(), httpClient);
        }

        [Test]
        public async Task Field_CRUD_Flow_Works()
        {
            // Arrange: build a Field with 2 MyBaseData entries
            Guid fieldId = Guid.NewGuid();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var field = new FieldModel
            {
                MetaInfo = new MetaInfo { ID = fieldId },
                Name = "Test Field",
            };

            // Create
            await api.PostFieldAsync(field);

            try
            {
                // Read: Get by id
                var fetched = await api.GetFieldByIdAsync(fieldId);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched.Name, Is.EqualTo(field.Name));

                // Read: Lists contain the new id/meta
                var ids = await api.GetAllFieldIdAsync();
                Assert.That(ids, Does.Contain(fieldId));

                var metas = await api.GetAllFieldMetaInfoAsync();
                Assert.That(metas, Is.Not.Null);
                Assert.That(metas.Any(m => m.ID == fieldId), Is.True);

                var heavies = await api.GetAllFieldAsync();
                Assert.That(heavies, Is.Not.Null);
                Assert.That(heavies.Any(f => f?.MetaInfo?.ID == fieldId), Is.True);

                var batchExport = await api.BatchExportFieldsAsync(new FieldBatchExportRequest
                {
                    Scope = FieldBatchExportScope.Selected,
                    FieldIDs = new[] { fieldId }
                });
                Assert.Multiple(() =>
                {
                    Assert.That(batchExport.FormatIdentifier, Is.EqualTo("OSDC.Drilling.Field.BatchExport"));
                    Assert.That(batchExport.SchemaVersion, Is.EqualTo(2));
                    Assert.That(batchExport.CatalogDependencies, Is.Not.Null);
                    Assert.That(batchExport.Fields.Select(exported => exported.MetaInfo.ID), Is.EqualTo(new[] { fieldId }));
                });

                try
                {
                    await api.BatchRestoreFieldsAsync(new FieldBatchRestoreRequest
                    {
                        ConflictPolicy = FieldBatchRestoreConflictPolicy.FailIfExists,
                        CatalogPolicy = FieldBatchCatalogRestorePolicy.MapExisting,
                        Document = batchExport
                    });
                    Assert.Fail("FailIfExists should reject an existing field UUID.");
                }
                catch (ApiException<FieldBatchErrorEnvelope> ex)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(ex.StatusCode, Is.EqualTo(409));
                        Assert.That(ex.Result.Error, Is.EqualTo("field_restore_conflict"));
                        Assert.That(ex.Result.Errors.Single().PositionIndex, Is.Zero);
                    });
                }

                var restore = await api.BatchRestoreFieldsAsync(new FieldBatchRestoreRequest
                {
                    ConflictPolicy = FieldBatchRestoreConflictPolicy.ReplaceExisting,
                    CatalogPolicy = FieldBatchCatalogRestorePolicy.MapExisting,
                    Document = batchExport
                });
                Assert.Multiple(() =>
                {
                    Assert.That(restore.CreatedCount, Is.Zero);
                    Assert.That(restore.ReplacedCount, Is.EqualTo(1));
                    Assert.That(restore.FieldIDs, Is.EqualTo(new[] { fieldId }));
                });

                // Update
                fetched.Name = "Test Field Updated";
                await api.PutFieldByIdAsync(fieldId, fetched.LastModificationDate!.Value, fetched);

                var updated = await api.GetFieldByIdAsync(fieldId);
                Assert.That(updated.Name, Is.EqualTo("Test Field Updated"));
            }
            finally
            {
                // Delete and verify 404
                await api.DeleteFieldByIdAsync(fieldId);
                FieldModel? shouldBeNull = null;
                try
                {
                    shouldBeNull = await api.GetFieldByIdAsync(fieldId);
                }
                catch (ApiException ex)
                {
                    Assert.That(ex.StatusCode, Is.EqualTo(404));
                }
                Assert.That(shouldBeNull, Is.Null);
            }
        }

        [Test]
        public async Task Field_POST_EmptyId_Returns_BadRequest()
        {
            var invalid = new FieldModel
            {
                MetaInfo = new MetaInfo { ID = Guid.Empty },
                Name = "Invalid Field",
            };

            bool badRequest = false;
            try
            {
                await api.PostFieldAsync(invalid);
            }
            catch (ApiException ex)
            {
                badRequest = true;
                Assert.That(ex.StatusCode, Is.EqualTo(400));
            }
            Assert.That(badRequest, Is.True);
        }

        [Test]
        public async Task Field_POST_Duplicate_Returns_Conflict()
        {
            var id = Guid.NewGuid();
            var field = new FieldModel
            {
                MetaInfo = new MetaInfo { ID = id },
                Name = "Duplicate Field",
            };

            await api.PostFieldAsync(field);
            try
            {
                bool conflict = false;
                try
                {
                    await api.PostFieldAsync(field);
                }
                catch (ApiException ex)
                {
                    conflict = true;
                    Assert.That(ex.StatusCode, Is.EqualTo(409));
                }
                Assert.That(conflict, Is.True);
            }
            finally
            {
                await api.DeleteFieldByIdAsync(id);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient?.Dispose();
        }
    }
}

