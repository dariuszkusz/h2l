namespace H2l.Domain.Tasks;

public enum HumanTaskStatus
{
    Pending = 0,
    InProgress = 1,
    NeedsAssignment = 2,
    Completed = 3,
    Cancelled = 4,
    Expired = 5
}
