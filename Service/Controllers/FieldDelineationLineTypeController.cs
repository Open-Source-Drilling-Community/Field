using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Field.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Field.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class FieldDelineationLineTypeController : ControllerBase
    {
        private readonly ILogger<FieldDelineationLineTypeManager> _logger;
        private readonly FieldDelineationLineTypeManager _manager;

        public FieldDelineationLineTypeController(ILogger<FieldDelineationLineTypeManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = FieldDelineationLineTypeManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllFieldDelineationLineTypeId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldDelineationLineTypeId()
        {
            var ids = _manager.GetAllFieldDelineationLineTypeId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllFieldDelineationLineTypeMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllFieldDelineationLineTypeMetaInfo()
        {
            var metaInfos = _manager.GetAllFieldDelineationLineTypeMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetFieldDelineationLineTypeById")]
        public ActionResult<Model.FieldDelineationLineType?> GetFieldDelineationLineTypeById(Guid id)
        {
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
            var data = _manager.GetAllFieldDelineationLineType();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostFieldDelineationLineType")]
        public ActionResult PostFieldDelineationLineType([FromBody] Model.FieldDelineationLineType? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetFieldDelineationLineTypeById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddFieldDelineationLineType(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutFieldDelineationLineTypeById")]
        public ActionResult PutFieldDelineationLineTypeById(Guid id, [FromBody] Model.FieldDelineationLineType? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetFieldDelineationLineTypeById(id) == null)
            {
                return NotFound();
            }

            return _manager.UpdateFieldDelineationLineTypeById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteFieldDelineationLineTypeById")]
        public ActionResult DeleteFieldDelineationLineTypeById(Guid id)
        {
            if (_manager.GetFieldDelineationLineTypeById(id) == null)
            {
                return NotFound();
            }

            return _manager.DeleteFieldDelineationLineTypeById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
