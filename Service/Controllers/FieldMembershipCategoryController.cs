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
    public class FieldMembershipCategoryController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover FieldMembershipCategory resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverFieldMembershipCategory")]
        public ActionResult<OSDC.Drilling.Field.Model.FieldMembershipCategoryDiscoveryPage> DiscoverFieldMembershipCategory([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null)
        {
            UsageStatisticsField.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Field.Model.FieldMembershipCategoryLight>(_discoveryConnections.GetConnection(), "FieldMembershipCategoryTable", "FieldMembershipCategory", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> {  }, out int total);
                return Ok(new OSDC.Drilling.Field.Model.FieldMembershipCategoryDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<FieldMembershipCategoryManager> _logger;
        private readonly FieldMembershipCategoryManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public FieldMembershipCategoryController(ILogger<FieldMembershipCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = FieldMembershipCategoryManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllFieldMembershipCategoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldMembershipCategoryId()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldMembershipCategoryIdPerDay();
            var ids = _manager.GetAllFieldMembershipCategoryId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllFieldMembershipCategoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllFieldMembershipCategoryMetaInfo()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldMembershipCategoryMetaInfoPerDay();
            var metaInfos = _manager.GetAllFieldMembershipCategoryMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetFieldMembershipCategoryById")]
        public ActionResult<Model.FieldMembershipCategory?> GetFieldMembershipCategoryById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementGetFieldMembershipCategoryByIdPerDay();
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetFieldMembershipCategoryById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [NonAction]
        public ActionResult<IEnumerable<Model.FieldMembershipCategory?>> GetAllFieldMembershipCategory()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldMembershipCategoryPerDay();
            var data = _manager.GetAllFieldMembershipCategory();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostFieldMembershipCategory")]
        [ProducesResponseType<Model.FieldMembershipCategory>(StatusCodes.Status200OK)]
        public ActionResult PostFieldMembershipCategory([FromBody] Model.FieldMembershipCategory? data)
        {
            UsageStatisticsField.Instance.IncrementPostFieldMembershipCategoryPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetFieldMembershipCategoryById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddFieldMembershipCategory(data)
                ? Ok(data)
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutFieldMembershipCategoryById")]
        [ProducesResponseType<Model.FieldMembershipCategory>(StatusCodes.Status200OK)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        public ActionResult PutFieldMembershipCategoryById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.FieldMembershipCategory? data)
        {
            UsageStatisticsField.Instance.IncrementPutFieldMembershipCategoryByIdPerDay();
            if (expectedModifiedUtc == default)
            {
                return BadRequest(new FieldMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            }
            return this.ToActionResult(FieldCatalogMutationManager.UpdateMembershipCategory(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id}", Name = "DeleteFieldMembershipCategoryById")]
        public ActionResult DeleteFieldMembershipCategoryById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementDeleteFieldMembershipCategoryByIdPerDay();
            return this.ToActionResult(FieldCatalogMutationManager.DeleteMembershipCategory(_connectionManager, _logger, id));
        }
    }
}
