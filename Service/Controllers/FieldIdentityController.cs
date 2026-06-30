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
    public class FieldIdentityController : ControllerBase
    {
        private readonly ILogger<FieldIdentityManager> _logger;
        private readonly FieldIdentityManager _manager;

        public FieldIdentityController(ILogger<FieldIdentityManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = FieldIdentityManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllFieldIdentityId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldIdentityId()
        {
            var ids = _manager.GetAllFieldIdentityId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllFieldIdentityMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllFieldIdentityMetaInfo()
        {
            var metaInfos = _manager.GetAllFieldIdentityMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetFieldIdentityById")]
        public ActionResult<Model.FieldIdentity?> GetFieldIdentityById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetFieldIdentityById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [HttpGet("HeavyData", Name = "GetAllFieldIdentity")]
        public ActionResult<IEnumerable<Model.FieldIdentity?>> GetAllFieldIdentity()
        {
            var data = _manager.GetAllFieldIdentity();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostFieldIdentity")]
        public ActionResult PostFieldIdentity([FromBody] Model.FieldIdentity? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetFieldIdentityById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddFieldIdentity(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutFieldIdentityById")]
        public ActionResult PutFieldIdentityById(Guid id, [FromBody] Model.FieldIdentity? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetFieldIdentityById(id) == null)
            {
                return NotFound();
            }

            return _manager.UpdateFieldIdentityById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteFieldIdentityById")]
        public ActionResult DeleteFieldIdentityById(Guid id)
        {
            if (_manager.GetFieldIdentityById(id) == null)
            {
                return NotFound();
            }

            return _manager.DeleteFieldIdentityById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
