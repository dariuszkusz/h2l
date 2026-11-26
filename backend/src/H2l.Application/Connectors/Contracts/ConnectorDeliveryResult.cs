namespace H2l.Application.Connectors.Contracts;

public sealed record ConnectorDeliveryResult(
    bool IsSuccess,
    bool IsTransientError,
    int? HttpStatusCode,
    TimeSpan? RetryAfter,
    string? ErrorMessage)
{
    public static ConnectorDeliveryResult Success(int? httpStatusCode = null)
        => new(true, false, httpStatusCode, null, null);

    public static ConnectorDeliveryResult TransientFailure(
        string errorMessage,
        int? httpStatusCode = null,
        TimeSpan? retryAfter = null)
        => new(false, true, httpStatusCode, retryAfter, errorMessage);

    public static ConnectorDeliveryResult PermanentFailure(
        string errorMessage,
        int? httpStatusCode = null)
        => new(false, false, httpStatusCode, null, errorMessage);

    public static ConnectorDeliveryResult ClassifyHttpFailure(
        int statusCode,
        string reasonPhrase,
        TimeSpan? retryAfter = null)
    {
        var isTransient = statusCode == 408 || statusCode == 429 || statusCode >= 500;
        var safeMessage = $"Remote workflow engine responded with HTTP {statusCode} ({reasonPhrase}).";
        return isTransient
            ? TransientFailure(safeMessage, statusCode, retryAfter)
            : PermanentFailure(safeMessage, statusCode);
    }
}
