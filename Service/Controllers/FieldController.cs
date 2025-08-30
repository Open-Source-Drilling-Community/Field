using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using NORCE.Drilling.Field.Service.Managers;
using NORCE.Drilling.Field.Model;

namespace NORCE.Drilling.Field.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class FieldController : ControllerBase
    {
        private readonly ILogger<FieldManager> _logger;
        private readonly FieldManager _fieldManager;

        public FieldController(ILogger<FieldManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _fieldManager = FieldManager.GetInstance(_logger, connectionManager);
        }

        /// <summary>
        /// Returns the list of Guid of all Field present in the microservice database at endpoint Field/api/Field
        /// </summary>
        /// <returns>the list of Guid of all Field present in the microservice database at endpoint Field/api/Field</returns>
        [HttpGet(Name = "GetAllFieldId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldId()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldIdPerDay();
            var ids = _fieldManager.GetAllFieldId();
            if (ids != null)
            {
                return Ok(ids);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the list of MetaInfo of all Field present in the microservice database, at endpoint Field/api/Field/MetaInfo
        /// </summary>
        /// <returns>the list of MetaInfo of all Field present in the microservice database, at endpoint Field/api/Field/MetaInfo</returns>
        [HttpGet("MetaInfo", Name = "GetAllFieldMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllFieldMetaInfo()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldMetaInfoPerDay();
            var vals = _fieldManager.GetAllFieldMetaInfo();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the Field identified by its Guid from the microservice database, at endpoint Field/api/Field/MetaInfo/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the Field identified by its Guid from the microservice database, at endpoint Field/api/Field/MetaInfo/id</returns>
        [HttpGet("{id}", Name = "GetFieldById")]
        public ActionResult<Model.Field?> GetFieldById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementGetFieldByIdPerDay();
            if (!id.Equals(Guid.Empty))
            {
                var val = _fieldManager.GetFieldById(id);
                if (val != null)
                {
                    return Ok(val);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return BadRequest();
            }
        }


        /// <summary>
        /// Returns the list of all Field present in the microservice database, at endpoint Field/api/Field/HeavyData
        /// </summary>
        /// <returns>the list of all Field present in the microservice database, at endpoint Field/api/Field/HeavyData</returns>
        [HttpGet("HeavyData", Name = "GetAllField")]
        public ActionResult<IEnumerable<Model.Field?>> GetAllField()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldPerDay();
            var vals = _fieldManager.GetAllField();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Performs calculation on the given Field and adds it to the microservice database, at the endpoint Field/api/Field
        /// </summary>
        /// <param name="field"></param>
        /// <returns>true if the given Field has been added successfully to the microservice database, at the endpoint Field/api/Field</returns>
        [HttpPost(Name = "PostField")]
        public ActionResult PostField([FromBody] Model.Field? data)
        {
            UsageStatisticsField.Instance.IncrementPostFieldPerDay();
            // Check if field exists in the database through ID
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID != Guid.Empty)
            {
                var existingData = _fieldManager.GetFieldById(data.MetaInfo.ID);
                if (existingData == null)
                {   
                    //  If field was not found, call AddField, where the field.Calculate()
                    // method is called. 
                    if (_fieldManager.AddField(data))
                    {
                        return Ok(); // status=OK is used rather than status=Created because NSwag auto-generated controllers use 200 (OK) rather than 201 (Created) as return codes
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given Field already exists and will not be added");
                    return StatusCode(StatusCodes.Status409Conflict);
                }
            }
            else
            {
                _logger.LogWarning("The given Field is null, badly formed, or its ID is empty");
                return BadRequest();
            }
        }

        /// <summary>
        /// Performs calculation on the given Field and updates it in the microservice database, at the endpoint Field/api/Field/id
        /// </summary>
        /// <param name="field"></param>
        /// <returns>true if the given Field has been updated successfully to the microservice database, at the endpoint Field/api/Field/id</returns>
        [HttpPut("{id}", Name = "PutFieldById")]
        public ActionResult PutFieldById(Guid id, [FromBody] Model.Field? data)
        {
            UsageStatisticsField.Instance.IncrementPutFieldByIdPerDay();
            // Check if Field is in the data base
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID.Equals(id))
            {
                var existingData = _fieldManager.GetFieldById(id);
                if (existingData != null)
                {
                    if (_fieldManager.UpdateFieldById(id, data))
                    {
                        return Ok();
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given Field has not been found in the database");
                    return NotFound();
                }
            }
            else
            {
                _logger.LogWarning("The given Field is null, badly formed, or its does not match the ID to update");
                return BadRequest();
            }
        }

        /// <summary>
        /// Deletes the Field of given ID from the microservice database, at the endpoint Field/api/Field/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the Field was deleted from the microservice database, at the endpoint Field/api/Field/id</returns>
        [HttpDelete("{id}", Name = "DeleteFieldById")]
        public ActionResult DeleteFieldById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementDeleteFieldByIdPerDay();
            if (_fieldManager.GetFieldById(id) != null)
            {
                if (_fieldManager.DeleteFieldById(id))
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                _logger.LogWarning("The Field of given ID does not exist");
                return NotFound();
            }
        }
    }
}
