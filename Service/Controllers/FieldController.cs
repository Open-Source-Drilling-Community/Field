using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Field.Service.Managers;
using OSDC.Drilling.Field.Model;

namespace OSDC.Drilling.Field.Service.Controllers
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
        /// Exports every stored field or an explicitly ordered selection together
        /// with its referenced Field-owned catalog definitions. The operation is
        /// read-only and never returns a partial selected batch.
        /// </summary>
        [HttpPost("BatchExport", Name = "BatchExportFields")]
        [ProducesResponseType<FieldBatchExportDocument>(StatusCodes.Status200OK)]
        [ProducesResponseType<FieldBatchErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<FieldBatchErrorEnvelope>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<FieldBatchErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult<FieldBatchExportDocument> BatchExportFields([FromBody] FieldBatchExportRequest? request)
        {
            UsageStatisticsField.Instance.IncrementBatchExportFieldsPerDay();
            FieldBatchExportOutcome outcome = _fieldManager.ExportBatch(request);
            if (outcome.IsSuccess)
            {
                return Ok(outcome.Document);
            }

            return outcome.FailureKind switch
            {
                FieldBatchExportFailureKind.InvalidRequest => BadRequest(outcome.Error),
                FieldBatchExportFailureKind.FieldNotFound => NotFound(outcome.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
            };
        }

        /// <summary>
        /// Resolves portable catalog references and atomically restores definitions,
        /// options and fields. FailIfExists rejects the complete transaction on any
        /// field UUID conflict; ReplaceExisting replaces existing fields together.
        /// </summary>
        [HttpPost("BatchRestore", Name = "BatchRestoreFields")]
        [ProducesResponseType<FieldBatchRestoreResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<FieldBatchErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<FieldBatchErrorEnvelope>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<FieldBatchErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult<FieldBatchRestoreResponse> BatchRestoreFields([FromBody] FieldBatchRestoreRequest? request)
        {
            UsageStatisticsField.Instance.IncrementBatchRestoreFieldsPerDay();
            FieldBatchRestoreOutcome outcome = _fieldManager.RestoreBatch(request);
            if (outcome.IsSuccess)
            {
                return Ok(outcome.Response);
            }

            return outcome.FailureKind switch
            {
                FieldBatchRestoreFailureKind.InvalidRequest => BadRequest(outcome.Error),
                FieldBatchRestoreFailureKind.Conflict => Conflict(outcome.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
            };
        }

        /// <summary>
        /// Returns the list of all FieldLight present in the microservice database, at endpoint Field/api/Field/LightData
        /// </summary>
        /// <returns>the list of all FieldLight present in the microservice database, at endpoint Field/api/Field/LightData</returns>
        [HttpGet("LightData", Name = "GetAllFieldLight")]
        public ActionResult<IEnumerable<Model.FieldLight>> GetAllFieldLight()
        {
            UsageStatisticsField.Instance.IncrementGetAllFieldLightPerDay();
            var vals = _fieldManager.GetAllFieldLight();
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
        [ProducesResponseType<Model.Field>(StatusCodes.Status200OK)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult PostField([FromBody] Model.Field? data)
        {
            UsageStatisticsField.Instance.IncrementPostFieldPerDay();
            return this.ToActionResult(_fieldManager.AddField(data), data);
        }

        /// <summary>
        /// Performs calculation on the given Field and updates it in the microservice database, at the endpoint Field/api/Field/id
        /// </summary>
        /// <param name="field"></param>
        /// <returns>true if the given Field has been updated successfully to the microservice database, at the endpoint Field/api/Field/id</returns>
        [HttpPut("{id}", Name = "PutFieldById")]
        [ProducesResponseType<Model.Field>(StatusCodes.Status200OK)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<FieldMutationErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult PutFieldById(Guid id, [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.Field? data)
        {
            UsageStatisticsField.Instance.IncrementPutFieldByIdPerDay();
            if (expectedModifiedUtc == default)
            {
                return BadRequest(new FieldMutationErrorEnvelope
                {
                    Error = "invalid_request",
                    Message = "expectedModifiedUtc is required."
                });
            }
            return this.ToActionResult(_fieldManager.UpdateFieldById(id, expectedModifiedUtc, data), data);
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
