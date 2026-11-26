namespace H2l.Application.Outbox.DTOs;

public sealed record DeadLetterListResponse(
    IReadOnlyList<DeadLetterItemResponse> Items);
