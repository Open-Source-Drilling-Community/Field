using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Field.Model;
using NORCE.Drilling.Field.Service.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NORCE.Drilling.Field.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class FieldCartographicConversionSetController : ControllerBase
    {
        private readonly ILogger<FieldCartographicConversionSetManager> _logger;
        private readonly FieldCartographicConversionSetManager _fieldCartographicConversionSetManager;

        public FieldCartographicConversionSetController(ILogger<FieldCartographicConversionSetManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _fieldCartographicConversionSetManager = FieldCartographicConversionSetManager.GetInstance(_logger, connectionManager);
        }

        /// <summary>
        /// Returns the list of Guid of all FieldCartographicConversionSet present in the microservice database at endpoint CartographicProjection/api/CartographicConversionSet
        /// </summary>
        /// <returns>the list of Guid of all FieldCartographicConversionSet present in the microservice database at endpoint CartographicProjection/api/CartographicConversionSet</returns>
        [HttpGet(Name = "GetAllFieldCartographicConversionSetId")]
        public ActionResult<IEnumerable<Guid>> GetAllFieldCartographicConversionSetId()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldCartographicConversionSetIdPerDay();
            var ids = _fieldCartographicConversionSetManager.GetAllFieldCartographicConversionSetId();
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
        /// Returns the list of MetaInfo of all FieldCartographicConversionSet present in the microservice database, at endpoint CartographicProjection/api/CartographicConversionSet/MetaInfo
        /// </summary>
        /// <returns>the list of MetaInfo of all FieldCartographicConversionSet present in the microservice database, at endpoint CartographicProjection/api/CartographicConversionSet/MetaInfo</returns>
        [HttpGet("MetaInfo", Name = "GetAllFieldCartographicConversionSetMetaInfo")]
        public ActionResult<IEnumerable<ModelShared.MetaInfo>> GetAllFieldCartographicConversionSetMetaInfo()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldCartographicConversionSetMetaInfoPerDay();
            var vals = _fieldCartographicConversionSetManager.GetAllFieldCartographicConversionSetMetaInfo();
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
        /// Returns the FieldCartographicConversionSet identified by its Guid from the microservice database, at endpoint CartographicProjection/api/CartographicConversionSet/MetaInfo/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the FieldCartographicConversionSet identified by its Guid from the microservice database, at endpoint CartographicProjection/api/CartographicConversionSet/MetaInfo/id</returns>
        [HttpGet("{id}", Name = "GetFieldCartographicConversionSetById")]
        public ActionResult<Model.FieldCartographicConversionSet?> GetFieldCartographicConversionSetById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementGetFieldCartographicConversionSetByIdPerDay();
            if (!id.Equals(Guid.Empty))
            {
                var val = _fieldCartographicConversionSetManager.GetFieldCartographicConversionSetById(id);
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
        /// Returns the list of all FieldCartographicConversionSetLight present in the microservice database, at endpoint CartographicProjection/api/CartographicConversionSet/LightData
        /// </summary>
        /// <returns>the list of all FieldCartographicConversionSetLight present in the microservice database, at endpoint CartographicProjection/api/CartographicConversionSet/LightData</returns>
        [HttpGet("LightData", Name = "GetAllFieldCartographicConversionSetLight")]
        public ActionResult<IEnumerable<Model.FieldCartographicConversionSetLight>> GetAllFieldCartographicConversionSetLight()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldCartographicConversionSetLightPerDay();
            var vals = _fieldCartographicConversionSetManager.GetAllFieldCartographicConversionSetLight();
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
        /// Returns the list of all FieldCartographicConversionSet present in the microservice database, at endpoint CartographicProjection/api/CartographicConversionSet/HeavyData
        /// </summary>
        /// <returns>the list of all FieldCartographicConversionSet present in the microservice database, at endpoint CartographicProjection/api/CartographicConversionSet/HeavyData</returns>
        [HttpGet("HeavyData", Name = "GetAllFieldCartographicConversionSet")]
        public ActionResult<IEnumerable<Model.FieldCartographicConversionSet?>> GetAllFieldCartographicConversionSet()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldCartographicConversionSetPerDay();
            var vals = _fieldCartographicConversionSetManager.GetAllFieldCartographicConversionSet();
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
        /// Performs calculation on the given FieldCartographicConversionSet and adds it to the microservice database, at the endpoint CartographicProjection/api/CartographicConversionSet
        /// </summary>
        /// <param name="fieldCartographicConversionSet"></param>
        /// <returns>true if the given FieldCartographicConversionSet has been added successfully to the microservice database, at the endpoint CartographicProjection/api/CartographicConversionSet</returns>
        [HttpPost(Name = "PostFieldCartographicConversionSet")]
        public async Task<ActionResult> PostFieldCartographicConversionSet([FromBody] Model.FieldCartographicConversionSet? data)
        {
            UsageStatisticsField.Instance.IncrementPostFieldCartographicConversionSetPerDay();
            // Check if cartographicConversionSet exists in the database through ID
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID != Guid.Empty)
            {
                var existingData = _fieldCartographicConversionSetManager.GetFieldCartographicConversionSetById(data.MetaInfo.ID);
                if (existingData == null)
                {
                    //  If cartographicConversionSet was not found, call AddCartographicConversionSet, where the cartographicConversionSet.Calculate()
                    // method is called. 
                    if (await _fieldCartographicConversionSetManager.AddFieldCartographicConversionSet(data))
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
                    _logger.LogWarning("The given FieldCartographicConversionSet already exists and will not be added");
                    return StatusCode(StatusCodes.Status409Conflict);
                }
            }
            else
            {
                _logger.LogWarning("The given FieldCartographicConversionSet is null, badly formed, or its ID is empty");
                return BadRequest();
            }
        }

        /// <summary>
        /// Performs calculation on the given FieldCartographicConversionSet and updates it in the microservice database, at the endpoint CartographicProjection/api/CartographicConversionSet/id
        /// </summary>
        /// <param name="cartographicConversionSet"></param>
        /// <returns>true if the given FieldCartographicConversionSet has been updated successfully to the microservice database, at the endpoint CartographicProjection/api/CartographicConversionSet/id</returns>
        [HttpPut("{id}", Name = "PutFieldCartographicConversionSetById")]
        public async Task<ActionResult> PutFieldCartographicConversionSetById(Guid id, [FromBody] Model.FieldCartographicConversionSet? data)
        {
            UsageStatisticsField.Instance.IncrementPutFieldCartographicConversionSetByIdPerDay();
            // Check if FieldCartographicConversionSet is in the data base
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID.Equals(id))
            {
                var existingData = _fieldCartographicConversionSetManager.GetFieldCartographicConversionSetById(id);
                if (existingData != null)
                {
                    if (await _fieldCartographicConversionSetManager.UpdateFieldCartographicConversionSetById(id, data))
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
                    _logger.LogWarning("The given FieldCartographicConversionSet has not been found in the database");
                    return NotFound();
                }
            }
            else
            {
                _logger.LogWarning("The given FieldCartographicConversionSet is null, badly formed, or its does not match the ID to update");
                return BadRequest();
            }
        }

        /// <summary>
        /// Deletes the FieldCartographicConversionSet of given ID from the microservice database, at the endpoint CartographicProjection/api/CartographicConversionSet/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the FieldCartographicConversionSet was deleted from the microservice database, at the endpoint CartographicProjection/api/CartographicConversionSet/id</returns>
        [HttpDelete("{id}", Name = "DeleteFieldCartographicConversionSetById")]
        public ActionResult DeleteFieldCartographicConversionSetById(Guid id)
        {
            UsageStatisticsField.Instance.IncrementDeleteFieldCartographicConversionSetByIdPerDay();
            if (_fieldCartographicConversionSetManager.GetFieldCartographicConversionSetById(id) != null)
            {
                if (_fieldCartographicConversionSetManager.DeleteFieldCartographicConversionSetById(id))
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
                _logger.LogWarning("The FieldCartographicConversionSet of given ID does not exist");
                return NotFound();
            }
        }
    }
}
