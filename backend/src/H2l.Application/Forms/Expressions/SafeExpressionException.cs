namespace H2l.Application.Forms.Expressions;

public sealed class SafeExpressionException : Exception
{
    public SafeExpressionException(string message)
        : base(message)
    {
    }
}
