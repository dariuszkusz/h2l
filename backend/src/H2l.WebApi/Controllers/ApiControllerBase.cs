using H2l.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace H2l.WebApi.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ToProblemResult(result.Error);
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return ToProblemResult(result.Error);
    }

    protected IActionResult ToCreatedResult<T>(Result<T> result, string? location = null)
    {
        if (result.IsSuccess)
        {
            return location is null
                ? StatusCode(StatusCodes.Status201Created, result.Value)
                : Created(location, result.Value);
        }

        return ToProblemResult(result.Error);
    }

    protected ObjectResult ToProblemResult(Error error)
    {
        var (statusCode, title, type) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation Error", "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized", "https://tools.ietf.org/html/rfc9110#section-15.5.2"),
            ErrorType.PaymentRequired => (StatusCodes.Status402PaymentRequired, "Payment Required", "https://tools.ietf.org/html/rfc9110#section-15.5.3"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden", "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found", "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict", "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            ErrorType.PayloadTooLarge => (StatusCodes.Status413PayloadTooLarge, "Payload Too Large", "https://tools.ietf.org/html/rfc9110#section-15.5.14"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = error.Message,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = error.Code;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}
