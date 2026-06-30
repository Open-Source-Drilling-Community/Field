using NORCE.Drilling.Field.ModelShared;

namespace ServiceTest
{
    public class FieldIdentityControllerTests
    {
        private static string host = "https://localhost:5001/";
        private static string basePath = "Field/api/";

        [Test]
        public async Task FI_CRUD_Flow_Works()
        {
            var api = new Client(host + basePath, new HttpClient());
            FieldIdentity identity = new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = "test field identity"
            };

            await api.PostFieldIdentityAsync(identity);
            FieldIdentity fetched = await api.GetFieldIdentityByIdAsync(identity.MetaInfo.ID);
            Assert.That(fetched.Name, Is.EqualTo(identity.Name));

            fetched.Name = "updated field identity";
            await api.PutFieldIdentityByIdAsync(fetched.MetaInfo.ID, fetched);
            FieldIdentity updated = await api.GetFieldIdentityByIdAsync(fetched.MetaInfo.ID);
            Assert.That(updated.Name, Is.EqualTo("updated field identity"));

            await api.DeleteFieldIdentityByIdAsync(fetched.MetaInfo.ID);
            Assert.ThrowsAsync<ApiException>(async () => await api.GetFieldIdentityByIdAsync(fetched.MetaInfo.ID));
        }

        [Test]
        public async Task FI_GET_Collection_Endpoints_Return_OK()
        {
            var api = new Client(host + basePath, new HttpClient());

            var ids = await api.GetAllFieldIdentityIdAsync();
            var metas = await api.GetAllFieldIdentityMetaInfoAsync();
            var heavy = await api.GetAllFieldIdentityAsync();

            Assert.That(ids, Is.Not.Null);
            Assert.That(metas, Is.Not.Null);
            Assert.That(heavy, Is.Not.Null);
        }

        [Test]
        public void FI_POST_EmptyId_Returns_BadRequest()
        {
            var api = new Client(host + basePath, new HttpClient());
            FieldIdentity invalid = new()
            {
                MetaInfo = new MetaInfo { ID = Guid.Empty },
                Name = "bad"
            };

            Assert.ThrowsAsync<ApiException>(async () => await api.PostFieldIdentityAsync(invalid));
        }
    }
}
