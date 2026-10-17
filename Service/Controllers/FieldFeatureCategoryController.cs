using OSDC.Drilling.Field.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Field.Model;
using OSDC.Drilling.Field.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Field.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class FieldFeatureCategoryController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover FieldFeatureCategory resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverFieldFeatureCategory")]
        public ActionResult<OSDC.Drilling.Field.Model.FieldFeatureCategoryDiscoveryPage> DiscoverFieldFeatureCategory([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null)
        {
            UsageStatisticsField.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Field.Model.FieldFeatureCategoryLight>(_discoveryConnections.GetConnection(), "FieldFeatureCategoryTable", "FieldFeatureCategory", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> {  }, out int total);
                return Ok(new OSDC.Drilling.Field.Model.FieldFeatureCategoryDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<FieldFeatureCategoryManager> _logger;
        private readonly FieldFeatureCategoryManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public FieldFeatureCategoryController(ILogger<FieldFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = FieldFeatureCategoryManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllFieldFeatureCategoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldFeatureCategoryId()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldFeatureCategoryIdPerDay();
            var ids = _manager.GetAllFieldFeatureCategoryId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllFieldFeatureCategoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllFieldFeatureCategoryMetaInfo()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldFeatureCategoryMetaInfoPerDay();
            var metaInfos = _manager.GetAllFieldFeatureCategoryMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetFieldFeatureCategoryById")]
        public ActionResult<Model.FieldFeatureCategory?> GetFieldFeatureCategoryById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementGetFieldFeatureCategoryByIdPerDay();
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetFieldFeatureCategoryById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [NonAction]
        public ActionResult<IEnumerable<Model.FieldFeatureCategory?>> GetAllFieldFeatureCategory()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldFeatureCategoryPerDay();
            var data = _manager.GetAllFieldFeatureCategory();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostFieldFeatureCategory")]
        [ProducesResponseType<Model.FieldFeatureCategory>(StatusCodes.Status200OK)]
        public ActionResult PostFieldFeatureCategory([FromBody] Model.FieldFeatureCategory? data)
        {
            UsageStatisticsField.Instance.IncrementPostFieldFeatureCategoryPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetFieldFeatureCategoryById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddFieldFeatureCategory(data)
                ? Ok(data)
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutFieldFeatureCategoryById")]
        [ProducesResponseType<Model.FieldFeatureCategory>(StatusCodes.Status200OK)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        public ActionResult PutFieldFeatureCategoryById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.FieldFeatureCategory? data)
        {
            UsageStatisticsField.Instance.IncrementPutFieldFeatureCategoryByIdPerDay();
            if (expectedModifiedUtc == default)
            {
                return BadRequest(new FieldMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            }
            return this.ToActionResult(FieldCatalogMutationManager.UpdateFeatureCategory(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id}", Name = "DeleteFieldFeatureCategoryById")]
        public ActionResult DeleteFieldFeatureCategoryById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementDeleteFieldFeatureCategoryByIdPerDay();
            return this.ToActionResult(FieldCatalogMutationManager.DeleteFeatureCategory(_connectionManager, _logger, id));
        }
    }
}
