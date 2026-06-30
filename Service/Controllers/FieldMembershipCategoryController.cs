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
    public class FieldMembershipCategoryController : ControllerBase
    {
        private readonly ILogger<FieldMembershipCategoryManager> _logger;
        private readonly FieldMembershipCategoryManager _manager;

        public FieldMembershipCategoryController(ILogger<FieldMembershipCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = FieldMembershipCategoryManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllFieldMembershipCategoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldMembershipCategoryId()
        {
            var ids = _manager.GetAllFieldMembershipCategoryId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllFieldMembershipCategoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllFieldMembershipCategoryMetaInfo()
        {
            var metaInfos = _manager.GetAllFieldMembershipCategoryMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetFieldMembershipCategoryById")]
        public ActionResult<Model.FieldMembershipCategory?> GetFieldMembershipCategoryById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetFieldMembershipCategoryById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [HttpGet("HeavyData", Name = "GetAllFieldMembershipCategory")]
        public ActionResult<IEnumerable<Model.FieldMembershipCategory?>> GetAllFieldMembershipCategory()
        {
            var data = _manager.GetAllFieldMembershipCategory();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostFieldMembershipCategory")]
        public ActionResult PostFieldMembershipCategory([FromBody] Model.FieldMembershipCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetFieldMembershipCategoryById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddFieldMembershipCategory(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutFieldMembershipCategoryById")]
        public ActionResult PutFieldMembershipCategoryById(Guid id, [FromBody] Model.FieldMembershipCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetFieldMembershipCategoryById(id) == null)
            {
                return NotFound();
            }

            return _manager.UpdateFieldMembershipCategoryById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteFieldMembershipCategoryById")]
        public ActionResult DeleteFieldMembershipCategoryById(Guid id)
        {
            if (_manager.GetFieldMembershipCategoryById(id) == null)
            {
                return NotFound();
            }

            return _manager.DeleteFieldMembershipCategoryById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
