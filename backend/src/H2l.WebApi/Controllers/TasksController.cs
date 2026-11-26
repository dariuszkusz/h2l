using H2l.Application.Tasks.Commands;
using H2l.Application.Tasks.DTOs;
using H2l.Application.Tasks.Queries;
using H2l.Domain.Common;
using H2l.Domain.Tasks;
using H2l.Infrastructure.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wolverine;

namespace H2l.WebApi.Controllers;

[Route("api/v1/tasks")]
[Tags("Tasks")]
[Authorize]
public sealed class TasksController : ApiControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly IOptions<AttachmentsOptions> _attachmentsOptions;

    public TasksController(
        IMessageBus messageBus,
        IOptions<AttachmentsOptions> attachmentsOptions)
    {
        _messageBus = messageBus;
        _attachmentsOptions = attachmentsOptions;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TaskListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> ListTasks(
        [FromQuery] HumanTaskStatus? status = null,
        [FromQuery] string? scope = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseScope(scope, out var parsedScope))
        {
            return ToProblemResult(Error.Validation(
                "TASK_SCOPE_INVALID",
                "Scope must be one of: all, assigned, group_queue."));
        }

        var result = await _messageBus.InvokeAsync<Result<TaskListResponse>>(
            new ListTasksQuery(status, parsedScope, page, pageSize),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType(typeof(TaskDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTaskDetail(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<TaskDetailResponse>>(
            new GetTaskDetailQuery(taskId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{taskId:guid}/claim")]
    [ProducesResponseType(typeof(TaskDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ClaimTask(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<TaskDetailResponse>>(
            new ClaimTaskCommand(taskId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{taskId:guid}/unclaim")]
    [ProducesResponseType(typeof(TaskDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UnclaimTask(
        [FromRoute] Guid taskId,
        CancellationToken cancellationToken)
    {
        var result = await _messageBus.InvokeAsync<Result<TaskDetailResponse>>(
            new UnclaimTaskCommand(taskId),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{taskId:guid}/submit")]
    [ProducesResponseType(typeof(SubmitTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> SubmitTask(
        [FromRoute] Guid taskId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] SubmitTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return ToProblemResult(Error.Validation(
                "IDEMPOTENCY_KEY_REQUIRED",
                "Idempotency-Key header is required."));
        }

        var result = await _messageBus.InvokeAsync<Result<SubmitTaskResponse>>(
            new SubmitTaskCommand(taskId, idempotencyKey, request.Values, request.AttachmentIds),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpPost("{taskId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AttachmentUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    public async Task<IActionResult> UploadTaskAttachment(
        [FromRoute] Guid taskId,
        [FromForm] IFormFile? file,
        [FromForm] string? fieldVariableKey,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ToProblemResult(Error.Validation("FILE_REQUIRED", "A non-empty file is required."));
        }

        if (string.IsNullOrWhiteSpace(fieldVariableKey))
        {
            return ToProblemResult(Error.Validation("FIELD_VARIABLE_KEY_REQUIRED", "fieldVariableKey is required."));
        }

        var maxBytes = _attachmentsOptions.Value.MaxSizeMb * 1024L * 1024L;
        if (file.Length > maxBytes)
        {
            return ToProblemResult(Error.PayloadTooLarge(
                "ATTACHMENT_TOO_LARGE",
                $"Attachment exceeds maximum allowed size of {maxBytes} bytes."));
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var content = stream.ToArray();

        var result = await _messageBus.InvokeAsync<Result<AttachmentUploadResponse>>(
            new UploadTaskAttachmentCommand(
                taskId,
                fieldVariableKey,
                file.FileName,
                file.ContentType,
                file.Length,
                content),
            cancellationToken);

        return ToCreatedResult(
            result,
            result.IsSuccess ? $"/api/v1/tasks/{taskId}/attachments/{result.Value.Id}" : null);
    }

    private static bool TryParseScope(string? scope, out TaskListScope parsedScope)
    {
        if (string.IsNullOrWhiteSpace(scope) || string.Equals(scope, "all", StringComparison.OrdinalIgnoreCase))
        {
            parsedScope = TaskListScope.All;
            return true;
        }

        if (string.Equals(scope, "assigned", StringComparison.OrdinalIgnoreCase))
        {
            parsedScope = TaskListScope.Assigned;
            return true;
        }

        if (string.Equals(scope, "group_queue", StringComparison.OrdinalIgnoreCase)
            || string.Equals(scope, "groupQueue", StringComparison.OrdinalIgnoreCase))
        {
            parsedScope = TaskListScope.GroupQueue;
            return true;
        }

        parsedScope = TaskListScope.All;
        return false;
    }
}
