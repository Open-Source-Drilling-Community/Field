using NORCE.Drilling.Field.ModelShared;

namespace ServiceTest
{
    public class FieldMembershipCategoryControllerTests
    {
        private static string host = "https://localhost:5001/";
        private static string basePath = "Field/api/";

        [Test]
        public async Task FMC_CRUD_Flow_Works()
        {
            var api = new Client(host + basePath, new HttpClient());
            FieldMembershipCategory category = new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = "test membership category",
                IsExclusive = true,
                HasValidityPeriod = true,
                Options = [new FieldMembershipOption { ID = Guid.NewGuid(), Name = "test option" }]
            };

            await api.PostFieldMembershipCategoryAsync(category);
            FieldMembershipCategory fetched = await api.GetFieldMembershipCategoryByIdAsync(category.MetaInfo.ID);
            Assert.That(fetched.Name, Is.EqualTo(category.Name));
            Assert.That(fetched.Options, Has.Count.EqualTo(1));

            fetched.Name = "updated membership category";
            await api.PutFieldMembershipCategoryByIdAsync(fetched.MetaInfo.ID, fetched);
            FieldMembershipCategory updated = await api.GetFieldMembershipCategoryByIdAsync(fetched.MetaInfo.ID);
            Assert.That(updated.Name, Is.EqualTo("updated membership category"));

            await api.DeleteFieldMembershipCategoryByIdAsync(fetched.MetaInfo.ID);
            Assert.ThrowsAsync<ApiException>(async () => await api.GetFieldMembershipCategoryByIdAsync(fetched.MetaInfo.ID));
        }

        [Test]
        public async Task FMC_GET_Collection_Endpoints_Return_OK()
        {
            var api = new Client(host + basePath, new HttpClient());

            var ids = await api.GetAllFieldMembershipCategoryIdAsync();
            var metas = await api.GetAllFieldMembershipCategoryMetaInfoAsync();
            var heavy = await api.GetAllFieldMembershipCategoryAsync();

            Assert.That(ids, Is.Not.Null);
            Assert.That(metas, Is.Not.Null);
            Assert.That(heavy, Is.Not.Null);
        }

        [Test]
        public void FMC_POST_EmptyId_Returns_BadRequest()
        {
            var api = new Client(host + basePath, new HttpClient());
            FieldMembershipCategory invalid = new()
            {
                MetaInfo = new MetaInfo { ID = Guid.Empty },
                Name = "bad"
            };

            Assert.ThrowsAsync<ApiException>(async () => await api.PostFieldMembershipCategoryAsync(invalid));
        }
    }
}
