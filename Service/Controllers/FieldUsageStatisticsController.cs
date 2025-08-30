using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Field.Model;

namespace NORCE.Drilling.Field.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class FieldUsageStatisticsController : ControllerBase
    {
        private readonly ILogger _logger;

        public FieldUsageStatisticsController(ILogger<FieldUsageStatisticsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns the usage statistics present in the microservice database at endpoint Field/api/FieldUsageStatistics
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "GetFieldUsageStatistics")]
        public ActionResult<UsageStatisticsField> GetFieldUsageStatistics()
        {
            if (UsageStatisticsField.Instance != null)
            {
                return Ok(UsageStatisticsField.Instance);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
