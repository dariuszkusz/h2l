namespace H2l.Application.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
