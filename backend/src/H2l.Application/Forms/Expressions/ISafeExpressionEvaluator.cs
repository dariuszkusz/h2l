using H2l.Domain.Common;

namespace H2l.Application.Forms.Expressions;

public interface ISafeExpressionEvaluator
{
    Result ValidateSyntax(string expression);

    Result<object?> Evaluate(string expression, IReadOnlyDictionary<string, object?> context);

    Result<bool> EvaluateBoolean(string expression, IReadOnlyDictionary<string, object?> context);
}
