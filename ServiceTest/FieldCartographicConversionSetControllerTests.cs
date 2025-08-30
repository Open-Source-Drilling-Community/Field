using System.Net.Http.Headers;
using NORCE.Drilling.Field.ModelShared;

namespace ServiceTest
{
    public class FieldCartographicConversionSetControllerTests
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
        public async Task FCCS_GET_Collection_Endpoints_Return_OK()
        {
            var ids = await api.GetAllFieldCartographicConversionSetIdAsync();
            Assert.That(ids, Is.Not.Null);

            var metas = await api.GetAllFieldCartographicConversionSetMetaInfoAsync();
            Assert.That(metas, Is.Not.Null);

            var light = await api.GetAllFieldCartographicConversionSetLightAsync();
            Assert.That(light, Is.Not.Null);

            var heavy = await api.GetAllFieldCartographicConversionSetAsync();
            Assert.That(heavy, Is.Not.Null);
        }

        [Test]
        public async Task FCCS_GET_ById_Unknown_Returns_NotFound()
        {
            var id = Guid.NewGuid();
            FieldCartographicConversionSet? shouldBeNull = null;
            try
            {
                shouldBeNull = await api.GetFieldCartographicConversionSetByIdAsync(id);
            }
            catch (ApiException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(404));
            }
            Assert.That(shouldBeNull, Is.Null);
        }

        [Test]
        public async Task FCCS_POST_EmptyId_Returns_BadRequest()
        {
            var invalid = new FieldCartographicConversionSet
            {
                MetaInfo = new MetaInfo { ID = Guid.Empty },
                Name = "Invalid FCCS",
                Description = "Created by FCCS tests"
                // Note: FieldID and CartographicCoordinateList intentionally omitted here
            };

            bool badRequest = false;
            try
            {
                await api.PostFieldCartographicConversionSetAsync(invalid);
            }
            catch (ApiException ex)
            {
                badRequest = true;
                Assert.That(ex.StatusCode, Is.EqualTo(400));
            }
            Assert.That(badRequest, Is.True);
        }

        [Test]
        public async Task FCCS_DELETE_Unknown_Returns_NotFound()
        {
            var id = Guid.NewGuid();
            bool notFound = false;
            try
            {
                await api.DeleteFieldCartographicConversionSetByIdAsync(id);
            }
            catch (ApiException ex)
            {
                notFound = true;
                Assert.That(ex.StatusCode, Is.EqualTo(404));
            }
            Assert.That(notFound, Is.True);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            httpClient?.Dispose();
        }
    }
}

