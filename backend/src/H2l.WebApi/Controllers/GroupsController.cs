using H2l.Application.Groups.Commands;
using H2l.Application.Groups.DTOs;
using H2l.Application.Groups.Queries;
using H2l.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/groups")]
[Tags("UsersAndGroups")]
[Authorize]
public sealed class GroupsController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;

    public GroupsController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> ListGroups(CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<IReadOnlyList<GroupResponse>>>(
            new ListGroupsQuery(),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<GroupResponse>>(
            new CreateGroupCommand(request.Name, request.Code),
            cancellationToken);

        return ToCreatedResult(result, result.IsSuccess ? $"/api/v1/groups/{result.Value.Id}" : null);
    }

    [HttpGet("{groupId:guid}/members")]
    [Authorize(Roles = "Admin,ProcessDesigner")]
    [ProducesResponseType(typeof(IReadOnlyList<GroupMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ListGroupMembers(
        [FromRoute] Guid groupId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<IReadOnlyList<GroupMemberResponse>>>(
            new ListGroupMembersQuery(groupId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{groupId:guid}/members")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(GroupMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AddGroupMember(
        [FromRoute] Guid groupId,
        [FromBody] AddGroupMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<GroupMemberResponse>>(
            new AddGroupMemberCommand(groupId, request.UserId),
            cancellationToken);

        return ToCreatedResult(result, $"/api/v1/groups/{groupId}/members");
    }

    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> RemoveGroupMember(
        [FromRoute] Guid groupId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result>(
            new RemoveGroupMemberCommand(groupId, userId),
            cancellationToken);

        return ToActionResult(result);
    }
}
