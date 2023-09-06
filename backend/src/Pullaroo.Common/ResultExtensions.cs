using Microsoft.AspNetCore.Mvc;

using Orleans.FluentResults;

namespace Pullaroo.Common;

public static class ResultExtensions
{
    public static async Task<ActionResult<T>> ToActionResult<T>(this Task<Result<T>> resultTask)
    {
        var result = await resultTask;
        return result.IsSuccess && !result.Reasons.Any() ? new OkObjectResult(result.Value) : new BadRequestObjectResult(result.Reasons);
    }

    public static async Task<ActionResult> ToActionResult(this Task<Result> resultTask)
    {
        var result = await resultTask;
        return result.IsSuccess && !result.Reasons.Any() ? new OkResult() : new BadRequestObjectResult(result.Reasons);
    }
}
