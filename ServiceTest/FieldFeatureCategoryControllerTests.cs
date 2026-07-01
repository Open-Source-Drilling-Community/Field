using NORCE.Drilling.Field.ModelShared;

namespace ServiceTest
{
    public class FieldFeatureCategoryControllerTests
    {
        private static string host = "https://localhost:5001/";
        private static string basePath = "Field/api/";

        [Test]
        public async Task FFC_CRUD_Flow_Works()
        {
            var api = new Client(host + basePath, new HttpClient());
            FieldFeatureCategory category = new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = "test field feature category",
                IsExclusive = true,
                HasValidityPeriod = true,
                Options =
                [
                    new FieldFeatureOption { ID = Guid.NewGuid(), Name = "test option" }
                ]
            };

            await api.PostFieldFeatureCategoryAsync(category);
            FieldFeatureCategory fetched = await api.GetFieldFeatureCategoryByIdAsync(category.MetaInfo.ID);
            Assert.That(fetched.Name, Is.EqualTo(category.Name));
            Assert.That(fetched.Options, Has.Count.EqualTo(1));

            fetched.Name = "updated field feature category";
            await api.PutFieldFeatureCategoryByIdAsync(fetched.MetaInfo.ID, fetched);
            FieldFeatureCategory updated = await api.GetFieldFeatureCategoryByIdAsync(fetched.MetaInfo.ID);
            Assert.That(updated.Name, Is.EqualTo("updated field feature category"));

            await api.DeleteFieldFeatureCategoryByIdAsync(fetched.MetaInfo.ID);
            Assert.ThrowsAsync<ApiException>(async () => await api.GetFieldFeatureCategoryByIdAsync(fetched.MetaInfo.ID));
        }

        [Test]
        public async Task FFC_GET_Collection_Endpoints_Return_OK()
        {
            var api = new Client(host + basePath, new HttpClient());

            var ids = await api.GetAllFieldFeatureCategoryIdAsync();
            var metas = await api.GetAllFieldFeatureCategoryMetaInfoAsync();
            var heavy = await api.GetAllFieldFeatureCategoryAsync();

            Assert.That(ids, Is.Not.Null);
            Assert.That(metas, Is.Not.Null);
            Assert.That(heavy, Is.Not.Null);
        }

        [Test]
        public void FFC_POST_EmptyId_Returns_BadRequest()
        {
            var api = new Client(host + basePath, new HttpClient());
            FieldFeatureCategory invalid = new()
            {
                MetaInfo = new MetaInfo { ID = Guid.Empty },
                Name = "bad"
            };

            Assert.ThrowsAsync<ApiException>(async () => await api.PostFieldFeatureCategoryAsync(invalid));
        }
    }
}
