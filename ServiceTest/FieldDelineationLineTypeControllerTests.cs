using NORCE.Drilling.Field.ModelShared;

namespace ServiceTest
{
    public class FieldDelineationLineTypeControllerTests
    {
        private static string host = "https://localhost:5001/";
        private static string basePath = "Field/api/";

        [Test]
        public async Task FDLT_CRUD_Flow_Works()
        {
            var api = new Client(host + basePath, new HttpClient());
            FieldDelineationLineType lineType = new()
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = "test delineation line type"
            };

            await api.PostFieldDelineationLineTypeAsync(lineType);
            FieldDelineationLineType fetched = await api.GetFieldDelineationLineTypeByIdAsync(lineType.MetaInfo.ID);
            Assert.That(fetched.Name, Is.EqualTo(lineType.Name));

            fetched.Name = "updated delineation line type";
            await api.PutFieldDelineationLineTypeByIdAsync(fetched.MetaInfo.ID, fetched);
            FieldDelineationLineType updated = await api.GetFieldDelineationLineTypeByIdAsync(fetched.MetaInfo.ID);
            Assert.That(updated.Name, Is.EqualTo("updated delineation line type"));

            await api.DeleteFieldDelineationLineTypeByIdAsync(fetched.MetaInfo.ID);
            Assert.ThrowsAsync<ApiException>(async () => await api.GetFieldDelineationLineTypeByIdAsync(fetched.MetaInfo.ID));
        }

        [Test]
        public async Task FDLT_GET_Collection_Endpoints_Return_OK()
        {
            var api = new Client(host + basePath, new HttpClient());

            var ids = await api.GetAllFieldDelineationLineTypeIdAsync();
            var metas = await api.GetAllFieldDelineationLineTypeMetaInfoAsync();
            var heavy = await api.GetAllFieldDelineationLineTypeAsync();

            Assert.That(ids, Is.Not.Null);
            Assert.That(metas, Is.Not.Null);
            Assert.That(heavy, Is.Not.Null);
        }

        [Test]
        public void FDLT_POST_EmptyId_Returns_BadRequest()
        {
            var api = new Client(host + basePath, new HttpClient());
            FieldDelineationLineType invalid = new()
            {
                MetaInfo = new MetaInfo { ID = Guid.Empty },
                Name = "bad"
            };

            Assert.ThrowsAsync<ApiException>(async () => await api.PostFieldDelineationLineTypeAsync(invalid));
        }
    }
}
