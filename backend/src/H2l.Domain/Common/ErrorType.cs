namespace H2l.Domain.Common;

public enum ErrorType
{
    Validation = 0,
    NotFound = 1,
    Conflict = 2,
    Unauthorized = 3,
    Forbidden = 4,
    PaymentRequired = 5,
    Failure = 6
}
