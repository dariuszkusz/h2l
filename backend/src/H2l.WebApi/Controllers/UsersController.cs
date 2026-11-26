using H2l.Application.Auth.DTOs;
using H2l.Application.Users.Commands;
using H2l.Application.Users.DTOs;
using H2l.Application.Users.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/users")]
[Tags("UsersAndGroups")]
[Authorize]
public sealed class UsersController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public UsersController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> GetCurrentProfile(CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<UserProfileResponse>>(
            new GetCurrentUserProfileQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPatch("me/preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result>(
            new UpdateUserPreferencesCommand(request.PreferredLanguage, request.PreferredTheme),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<IReadOnlyList<UserSummaryResponse>>>(
            new ListUsersQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<CreateUserResponse>>(
            new CreateUserCommand(request.Email, request.ExternalId, request.SystemRole, request.GroupIds),
            cancellationToken);

        return ToCreatedResult(result, result.IsSuccess ? $"/api/v1/users/{result.Value.Id}" : null);
    }
}
