using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NORCE.Drilling.Field.Model;

namespace NORCE.Drilling.Field.Service.Controllers;

[Produces("application/json")]
[Route("FieldCoordinateConversion")]
[ApiController]
public sealed class FieldCoordinateConversionController(FieldCoordinateConversionService service) : ControllerBase
{
    [HttpPost("Forward", Name = "ForwardFieldCoordinates")]
    [ProducesResponseType<FieldCoordinateConversionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<FieldConversionErrorEnvelope>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<FieldConversionErrorEnvelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<FieldConversionErrorEnvelope>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<FieldConversionErrorEnvelope>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<FieldCoordinateConversionResponse>> Forward([FromBody] FieldForwardConversionRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await service.ForwardAsync(request, cancellationToken)); }
        catch (FieldConversionException exception) { return StatusCode(exception.StatusCode, exception.Envelope); }
    }

    [HttpPost("Inverse", Name = "InverseFieldCoordinates")]
    [ProducesResponseType<FieldCoordinateConversionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<FieldConversionErrorEnvelope>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<FieldConversionErrorEnvelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<FieldConversionErrorEnvelope>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<FieldConversionErrorEnvelope>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<FieldCoordinateConversionResponse>> Inverse([FromBody] FieldInverseConversionRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await service.InverseAsync(request, cancellationToken)); }
        catch (FieldConversionException exception) { return StatusCode(exception.StatusCode, exception.Envelope); }
    }
}
