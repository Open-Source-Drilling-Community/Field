using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.Field.Service.Managers;

namespace OSDC.Drilling.Field.Service.Controllers;

internal static class FieldMutationActionResults
{
    public static ActionResult ToActionResult(this ControllerBase controller, FieldMutationResult outcome) => outcome.FailureKind switch
    {
        FieldMutationFailureKind.None => controller.Ok(),
        FieldMutationFailureKind.InvalidRequest => controller.BadRequest(outcome.Error),
        FieldMutationFailureKind.NotFound => controller.NotFound(outcome.Error),
        FieldMutationFailureKind.Conflict => controller.Conflict(outcome.Error),
        _ => controller.StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
    };

    public static ActionResult ToActionResult<T>(this ControllerBase controller, FieldMutationResult outcome, T? successValue) =>
        outcome.FailureKind == FieldMutationFailureKind.None
            ? controller.Ok(successValue)
            : controller.ToActionResult(outcome);
}
