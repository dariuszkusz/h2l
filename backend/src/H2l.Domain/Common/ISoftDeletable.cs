namespace H2l.Domain.Common;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
}
