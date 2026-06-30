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
    public class FieldFeatureCategoryController : ControllerBase
    {
        private readonly ILogger<FieldFeatureCategoryManager> _logger;
        private readonly FieldFeatureCategoryManager _manager;

        public FieldFeatureCategoryController(ILogger<FieldFeatureCategoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = FieldFeatureCategoryManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllFieldFeatureCategoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldFeatureCategoryId()
        {
            var ids = _manager.GetAllFieldFeatureCategoryId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllFieldFeatureCategoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo?>> GetAllFieldFeatureCategoryMetaInfo()
        {
            var metaInfos = _manager.GetAllFieldFeatureCategoryMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetFieldFeatureCategoryById")]
        public ActionResult<Model.FieldFeatureCategory?> GetFieldFeatureCategoryById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var data = _manager.GetFieldFeatureCategoryById(id);
            return data != null ? Ok(data) : NotFound();
        }

        [HttpGet("HeavyData", Name = "GetAllFieldFeatureCategory")]
        public ActionResult<IEnumerable<Model.FieldFeatureCategory?>> GetAllFieldFeatureCategory()
        {
            var data = _manager.GetAllFieldFeatureCategory();
            return data != null ? Ok(data) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostFieldFeatureCategory")]
        public ActionResult PostFieldFeatureCategory([FromBody] Model.FieldFeatureCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                return BadRequest();
            }

            if (_manager.GetFieldFeatureCategoryById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return _manager.AddFieldFeatureCategory(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutFieldFeatureCategoryById")]
        public ActionResult PutFieldFeatureCategoryById(Guid id, [FromBody] Model.FieldFeatureCategory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetFieldFeatureCategoryById(id) == null)
            {
                return NotFound();
            }

            return _manager.UpdateFieldFeatureCategoryById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteFieldFeatureCategoryById")]
        public ActionResult DeleteFieldFeatureCategoryById(Guid id)
        {
            if (_manager.GetFieldFeatureCategoryById(id) == null)
            {
                return NotFound();
            }

            return _manager.DeleteFieldFeatureCategoryById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
