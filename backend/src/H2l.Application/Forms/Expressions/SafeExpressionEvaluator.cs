using System.Collections;
using System.Globalization;
using H2l.Domain.Common;

namespace H2l.Application.Forms.Expressions;

public sealed class SafeExpressionEvaluator : ISafeExpressionEvaluator
{
    public Result ValidateSyntax(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Result.Failure(Error.Validation("EXPRESSION_EMPTY", "Expression cannot be empty."));
        }

        try
        {
            _ = ExpressionParser.Parse(expression);
            return Result.Success();
        }
        catch (SafeExpressionException ex)
        {
            return Result.Failure(Error.Validation("EXPRESSION_SYNTAX_INVALID", ex.Message));
        }
    }

    public Result<object?> Evaluate(string expression, IReadOnlyDictionary<string, object?> context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return Result.Failure<object?>(Error.Validation("EXPRESSION_EMPTY", "Expression cannot be empty."));
        }

        try
        {
            var ast = ExpressionParser.Parse(expression);
            var value = EvaluateNode(ast, context ?? new Dictionary<string, object?>(), 0);
            return Result.Success(value);
        }
        catch (SafeExpressionException ex)
        {
            return Result.Failure<object?>(Error.Validation("EXPRESSION_EVALUATION_FAILED", ex.Message));
        }
    }

    public Result<bool> EvaluateBoolean(string expression, IReadOnlyDictionary<string, object?> context)
    {
        var result = Evaluate(expression, context);
        if (result.IsFailure)
        {
            return Result.Failure<bool>(result.Error);
        }

        return Result.Success(IsTruthy(result.Value));
    }

    private static object? EvaluateNode(AstNode node, IReadOnlyDictionary<string, object?> context, int depth)
    {
        if (depth > ExpressionParser.MaxAstDepth)
        {
            throw new SafeExpressionException($"Expression exceeds max AST depth ({ExpressionParser.MaxAstDepth}).");
        }

        return node switch
        {
            LiteralAstNode literal => literal.Value,
            IdentifierAstNode ident => context.TryGetValue(ident.Name, out var val) ? val : null,
            MemberAstNode member => EvaluateMember(member, context, depth + 1),
            UnaryAstNode unary => EvaluateUnary(unary, context, depth + 1),
            LogicalAstNode logical => EvaluateLogical(logical, context, depth + 1),
            TernaryAstNode ternary => IsTruthy(EvaluateNode(ternary.Condition, context, depth + 1))
                ? EvaluateNode(ternary.Consequent, context, depth + 1)
                : EvaluateNode(ternary.Alternate, context, depth + 1),
            CallAstNode call => EvaluateCall(call, context, depth + 1),
            BinaryAstNode binary => EvaluateBinary(binary, context, depth + 1),
            _ => throw new SafeExpressionException($"Unknown AST node type '{node.GetType().Name}'.")
        };
    }

    private static object? EvaluateMember(MemberAstNode member, IReadOnlyDictionary<string, object?> context, int depth)
    {
        var target = EvaluateNode(member.Target, context, depth);
        if (target is null)
        {
            return null;
        }

        if (ExpressionParser.ForbiddenProperties.Contains(member.Property))
        {
            throw new SafeExpressionException($"Access to forbidden property '{member.Property}' is blocked.");
        }

        if (member.Property == "length")
        {
            if (target is string str)
            {
                return (double)str.Length;
            }

            if (target is ICollection collection)
            {
                return (double)collection.Count;
            }

            if (target is IReadOnlyCollection<object?> readOnlyCollection)
            {
                return (double)readOnlyCollection.Count;
            }
        }

        if (target is IReadOnlyDictionary<string, object?> readOnlyDict &&
            readOnlyDict.TryGetValue(member.Property, out var readOnlyValue))
        {
            return readOnlyValue;
        }

        if (target is IDictionary<string, object?> dict &&
            dict.TryGetValue(member.Property, out var dictValue))
        {
            return dictValue;
        }

        return null;
    }

    private static object? EvaluateUnary(UnaryAstNode unary, IReadOnlyDictionary<string, object?> context, int depth)
    {
        var val = EvaluateNode(unary.Argument, context, depth);
        return unary.Operator switch
        {
            "!" => !IsTruthy(val),
            "-" => -ToNumber(val),
            _ => throw new SafeExpressionException($"Unsupported unary operator '{unary.Operator}'.")
        };
    }

    private static object? EvaluateLogical(LogicalAstNode logical, IReadOnlyDictionary<string, object?> context, int depth)
    {
        var left = EvaluateNode(logical.Left, context, depth);
        return logical.Operator switch
        {
            "&&" => IsTruthy(left) ? EvaluateNode(logical.Right, context, depth) : left,
            "||" => IsTruthy(left) ? left : EvaluateNode(logical.Right, context, depth),
            _ => throw new SafeExpressionException($"Unsupported logical operator '{logical.Operator}'.")
        };
    }

    private static object? EvaluateCall(CallAstNode call, IReadOnlyDictionary<string, object?> context, int depth)
    {
        if (!ExpressionParser.WhitelistedFunctions.Contains(call.Callee))
        {
            throw new SafeExpressionException($"Function '{call.Callee}' is not whitelisted.");
        }

        var args = new object?[call.Arguments.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            args[i] = EvaluateNode(call.Arguments[i], context, depth);
        }

        return call.Callee switch
        {
            "len" => InvokeLen(args.Length > 0 ? args[0] : null),
            "isEmpty" => InvokeIsEmpty(args.Length > 0 ? args[0] : null),
            "includes" => InvokeIncludes(args.Length > 0 ? args[0] : null, args.Length > 1 ? args[1] : null),
            "lower" => args.Length > 0 && args[0] is string ls ? ls.ToLowerInvariant() : string.Empty,
            "upper" => args.Length > 0 && args[0] is string us ? us.ToUpperInvariant() : string.Empty,
            _ => throw new SafeExpressionException($"Function '{call.Callee}' is not whitelisted.")
        };
    }

    private static object? EvaluateBinary(BinaryAstNode binary, IReadOnlyDictionary<string, object?> context, int depth)
    {
        var left = EvaluateNode(binary.Left, context, depth);
        var right = EvaluateNode(binary.Right, context, depth);

        return binary.Operator switch
        {
            "+" => EvaluateAdd(left, right),
            "-" => ToNumber(left) - ToNumber(right),
            "*" => ToNumber(left) * ToNumber(right),
            "/" => EvaluateDivide(left, right),
            "%" => EvaluateModulo(left, right),
            "<" => CompareOrdered(left, right) is < 0,
            "<=" => CompareOrdered(left, right) is <= 0,
            ">" => CompareOrdered(left, right) is > 0,
            ">=" => CompareOrdered(left, right) is >= 0,
            "==" => AreLooselyEqual(left, right),
            "!=" => !AreLooselyEqual(left, right),
            "===" => AreStrictlyEqual(left, right),
            "!==" => !AreStrictlyEqual(left, right),
            _ => throw new SafeExpressionException($"Unsupported binary operator '{binary.Operator}'.")
        };
    }

    private static object EvaluateAdd(object? left, object? right)
    {
        if (left is string || right is string)
        {
            return string.Concat(ToJsString(left), ToJsString(right));
        }

        return ToNumber(left) + ToNumber(right);
    }

    private static double EvaluateDivide(object? left, object? right)
    {
        var r = ToNumber(right);
        return r == 0d ? double.NaN : ToNumber(left) / r;
    }

    private static double EvaluateModulo(object? left, object? right)
    {
        var r = ToNumber(right);
        return r == 0d ? double.NaN : ToNumber(left) % r;
    }

    private static int? CompareOrdered(object? left, object? right)
    {
        if (left is string ls && right is string rs)
        {
            return string.CompareOrdinal(ls, rs);
        }

        var ln = ToNumber(left);
        var rn = ToNumber(right);
        if (double.IsNaN(ln) || double.IsNaN(rn))
        {
            return null;
        }

        return ln.CompareTo(rn);
    }

    private static bool AreStrictlyEqual(object? left, object? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            var ln = ToNumber(left);
            var rn = ToNumber(right);
            return !double.IsNaN(ln) && !double.IsNaN(rn) && ln.Equals(rn);
        }

        if (left is string ls && right is string rs)
        {
            return string.Equals(ls, rs, StringComparison.Ordinal);
        }

        if (left is bool lb && right is bool rb)
        {
            return lb == rb;
        }

        return Equals(left, right);
    }

    private static bool AreLooselyEqual(object? left, object? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (AreStrictlyEqual(left, right))
        {
            return true;
        }

        if ((IsNumeric(left) || left is bool) && (IsNumeric(right) || right is bool))
        {
            var ln = ToNumber(left);
            var rn = ToNumber(right);
            return !double.IsNaN(ln) && !double.IsNaN(rn) && ln.Equals(rn);
        }

        if ((left is string && IsNumeric(right)) || (IsNumeric(left) && right is string))
        {
            var ln = ToNumber(left);
            var rn = ToNumber(right);
            return !double.IsNaN(ln) && !double.IsNaN(rn) && ln.Equals(rn);
        }

        return false;
    }

    public static bool IsTruthy(object? value) => value switch
    {
        null => false,
        bool b => b,
        double d => !double.IsNaN(d) && d != 0d,
        float f => !float.IsNaN(f) && f != 0f,
        int i => i != 0,
        long l => l != 0,
        decimal m => m != 0m,
        string s => s.Length > 0,
        _ => true
    };

    private static bool IsNumeric(object? value)
        => value is double or float or int or long or decimal or short or byte;

    private static double ToNumber(object? value) => value switch
    {
        null => 0d,
        double d => d,
        float f => f,
        int i => i,
        long l => l,
        decimal m => (double)m,
        bool b => b ? 1d : 0d,
        string s when string.IsNullOrWhiteSpace(s) => 0d,
        string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => double.NaN
    };

    private static string ToJsString(object? value) => value switch
    {
        null => string.Empty,
        bool b => b ? "true" : "false",
        double d => d.ToString(CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static double InvokeLen(object? value) => value switch
    {
        string s => s.Length,
        ICollection c => c.Count,
        IReadOnlyCollection<object?> rc => rc.Count,
        _ => 0d
    };

    private static bool InvokeIsEmpty(object? value) => value switch
    {
        null => true,
        string s => string.IsNullOrWhiteSpace(s),
        ICollection c => c.Count == 0,
        IReadOnlyCollection<object?> rc => rc.Count == 0,
        _ => false
    };

    private static bool InvokeIncludes(object? collection, object? item)
    {
        if (collection is string s)
        {
            return s.Contains(ToJsString(item), StringComparison.Ordinal);
        }

        if (collection is IEnumerable enumerable)
        {
            foreach (var element in enumerable)
            {
                if (AreStrictlyEqual(element, item))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
