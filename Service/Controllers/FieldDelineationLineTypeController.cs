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
    public class FieldDelineationLineTypeController : ControllerBase
    {
        private readonly ILogger<FieldDelineationLineTypeManager> _logger;
        private readonly FieldDelineationLineTypeManager _manager;
        private readonly SqlConnectionManager _connectionManager;

        public FieldDelineationLineTypeController(ILogger<FieldDelineationLineTypeManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _manager = FieldDelineationLineTypeManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllFieldDelineationLineTypeId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldDelineationLineTypeId()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldDelineationLineTypeIdPerDay();
            var ids = _manager.GetAllFieldDelineationLineTypeId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllFieldDelineationLineTypeMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllFieldDelineationLineTypeMetaInfo()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldDelineationLineTypeMetaInfoPerDay();
            var metaInfos = _manager.GetAllFieldDelineationLineTypeMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetFieldDelineationLineTypeById")]
        public ActionResult<Model.FieldDelineationLineType?> GetFieldDelineationLineTypeById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementGetFieldDelineationLineTypeByIdPerDay();
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetFieldDelineationLineTypeById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [HttpGet("HeavyData", Name = "GetAllFieldDelineationLineType")]
        public ActionResult<IEnumerable<Model.FieldDelineationLineType?>> GetAllFieldDelineationLineType()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldDelineationLineTypePerDay();
            var data = _manager.GetAllFieldDelineationLineType();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostFieldDelineationLineType")]
        [ProducesResponseType<Model.FieldDelineationLineType>(StatusCodes.Status200OK)]
        public ActionResult PostFieldDelineationLineType([FromBody] Model.FieldDelineationLineType? data)
        {
            UsageStatisticsField.Instance.IncrementPostFieldDelineationLineTypePerDay();
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetFieldDelineationLineTypeById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddFieldDelineationLineType(data)
                ? Ok(data)
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutFieldDelineationLineTypeById")]
        [ProducesResponseType<Model.FieldDelineationLineType>(StatusCodes.Status200OK)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        public ActionResult PutFieldDelineationLineTypeById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.FieldDelineationLineType? data)
        {
            UsageStatisticsField.Instance.IncrementPutFieldDelineationLineTypeByIdPerDay();
            if (expectedModifiedUtc == default)
            {
                return BadRequest(new FieldMutationErrorEnvelope { Error = "invalid_request", Message = "expectedModifiedUtc is required." });
            }
            return this.ToActionResult(FieldCatalogMutationManager.UpdateDelineationLineType(_connectionManager, _logger, id, expectedModifiedUtc, data), data);
        }

        [HttpDelete("{id}", Name = "DeleteFieldDelineationLineTypeById")]
        public ActionResult DeleteFieldDelineationLineTypeById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementDeleteFieldDelineationLineTypeByIdPerDay();
            return this.ToActionResult(FieldCatalogMutationManager.DeleteDelineationLineType(_connectionManager, _logger, id));
        }
    }
}
