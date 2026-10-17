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
    public class FieldIdentityController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover FieldIdentity resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverFieldIdentity")]
        public ActionResult<OSDC.Drilling.Field.Model.FieldIdentityDiscoveryPage> DiscoverFieldIdentity([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null)
        {
            UsageStatisticsField.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Field.Model.FieldIdentityLight>(_discoveryConnections.GetConnection(), "FieldIdentityTable", "FieldIdentity", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> {  }, out int total);
                return Ok(new OSDC.Drilling.Field.Model.FieldIdentityDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<FieldIdentityManager> _logger;
        private readonly FieldIdentityManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public FieldIdentityController(ILogger<FieldIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = FieldIdentityManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllFieldIdentityId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldIdentityId()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldIdentityIdPerDay();
            var ids = _manager.GetAllFieldIdentityId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllFieldIdentityMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllFieldIdentityMetaInfo()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldIdentityMetaInfoPerDay();
            var metaInfos = _manager.GetAllFieldIdentityMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetFieldIdentityById")]
        public ActionResult<Model.FieldIdentity?> GetFieldIdentityById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementGetFieldIdentityByIdPerDay();
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetFieldIdentityById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [NonAction]
        public ActionResult<IEnumerable<Model.FieldIdentity?>> GetAllFieldIdentity()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldIdentityPerDay();
            var data = _manager.GetAllFieldIdentity();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostFieldIdentity")]
        [ProducesResponseType<Model.FieldIdentity>(StatusCodes.Status200OK)]
        public ActionResult PostFieldIdentity([FromBody] Model.FieldIdentity? data)
        {
            UsageStatisticsField.Instance.IncrementPostFieldIdentityPerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetFieldIdentityById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddFieldIdentity(data)
                ? Ok(data)
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutFieldIdentityById")]
        [ProducesResponseType<Model.FieldIdentity>(StatusCodes.Status200OK)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        public ActionResult PutFieldIdentityById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.FieldIdentity? data)
        {
            UsageStatisticsField.Instance.IncrementPutFieldIdentityByIdPerDay();
            if (expectedModifiedUtc == default)
            {
                return BadRequest(new FieldMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            }
            return this.ToActionResult(FieldCatalogMutationManager.UpdateIdentity(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id}", Name = "DeleteFieldIdentityById")]
        public ActionResult DeleteFieldIdentityById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementDeleteFieldIdentityByIdPerDay();
            return this.ToActionResult(FieldCatalogMutationManager.DeleteIdentity(_connectionManager, _logger, id));
        }
    }
}
