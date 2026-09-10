using Couppa.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Couppa.Api.Infrastructure;

/// <summary>
/// MVC does not automatically translate model-binding errors to the JSON error contract
/// because this project intentionally uses controllers with views instead of ApiController.
/// JSON/AJAX requests therefore need an explicit 422 response while normal Razor form posts
/// continue to redisplay their View with validation messages.
/// </summary>
public sealed class ApiModelStateValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (IsJsonRequest(context.HttpContext) && !context.ModelState.IsValid)
        {
            var details = context.ModelState
                .Where(pair => pair.Value?.Errors.Count > 0)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Giá trị không hợp lệ."
                            : error.ErrorMessage)
                        .ToArray());

            context.Result = new ObjectResult(ApiResponse<object>.Fail(
                "VALIDATION_ERROR",
                "Dữ liệu không hợp lệ.",
                details))
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            };
            return;
        }

        await next();
    }

    private static bool IsJsonRequest(HttpContext context) =>
        context.Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true
        || context.Request.Headers.Accept.Any(value =>
            value?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
}
