using System.Net.Http.Headers;
using NORCE.Drilling.Field.ModelShared;

namespace ServiceTest
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

            var field = new Field
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

                // Update
                fetched.Name = "Test Field Updated";
                await api.PutFieldByIdAsync(fieldId, fetched);

                var updated = await api.GetFieldByIdAsync(fieldId);
                Assert.That(updated.Name, Is.EqualTo("Test Field Updated"));
            }
            finally
            {
                // Delete and verify 404
                await api.DeleteFieldByIdAsync(fieldId);
                Field? shouldBeNull = null;
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
            var invalid = new Field
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
            var field = new Field
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

