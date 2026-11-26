namespace H2l.Application.Forms.Expressions;

public sealed class ExpressionParser
{
    public const int MaxAstDepth = 32;

    public static readonly IReadOnlySet<string> ForbiddenProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "__proto__",
        "prototype",
        "constructor",
        "__defineGetter__",
        "__defineSetter__",
        "__lookupGetter__",
        "__lookupSetter__"
    };

    public static readonly IReadOnlySet<string> WhitelistedFunctions = new HashSet<string>(StringComparer.Ordinal)
    {
        "len",
        "isEmpty",
        "includes",
        "lower",
        "upper"
    };

    private readonly IReadOnlyList<ExpressionToken> _tokens;
    private int _position;

    private ExpressionParser(IReadOnlyList<ExpressionToken> tokens)
    {
        _tokens = tokens;
    }

    public static AstNode Parse(string source)
    {
        var tokens = ExpressionTokenizer.Tokenize(source);
        var parser = new ExpressionParser(tokens);
        var ast = parser.ParseTernary(0);
        if (parser.Peek().Type != ExpressionTokenType.EndOfFile)
        {
            throw new SafeExpressionException($"Unexpected trailing token '{parser.Peek().Value}'.");
        }

        return ast;
    }

    private ExpressionToken Peek() => _tokens[_position];

    private ExpressionToken Consume() => _tokens[_position++];

    private ExpressionToken Expect(ExpressionTokenType type, string? expectedValue = null)
    {
        var token = Peek();
        if (token.Type != type || (expectedValue is not null && !string.Equals(token.Value?.ToString(), expectedValue, StringComparison.Ordinal)))
        {
            throw new SafeExpressionException($"Expected '{expectedValue ?? type.ToString()}', got '{token.Value ?? token.Type}'.");
        }

        return Consume();
    }

    private AstNode ParseTernary(int depth)
    {
        GuardDepth(depth);
        var condition = ParseOr(depth + 1);
        if (IsPunctuation("?"))
        {
            Consume();
            var consequent = ParseTernary(depth + 1);
            Expect(ExpressionTokenType.Punctuation, ":");
            var alternate = ParseTernary(depth + 1);
            return new TernaryAstNode(condition, consequent, alternate);
        }

        return condition;
    }

    private AstNode ParseOr(int depth)
    {
        GuardDepth(depth);
        var left = ParseAnd(depth + 1);
        while (IsOperator("||"))
        {
            var op = (string)Consume().Value!;
            var right = ParseAnd(depth + 1);
            left = new LogicalAstNode(op, left, right);
        }

        return left;
    }

    private AstNode ParseAnd(int depth)
    {
        GuardDepth(depth);
        var left = ParseEquality(depth + 1);
        while (IsOperator("&&"))
        {
            var op = (string)Consume().Value!;
            var right = ParseEquality(depth + 1);
            left = new LogicalAstNode(op, left, right);
        }

        return left;
    }

    private AstNode ParseEquality(int depth)
    {
        GuardDepth(depth);
        var left = ParseComparison(depth + 1);
        while (Peek().Type == ExpressionTokenType.Operator &&
               Peek().Value is "==" or "!=" or "===" or "!==")
        {
            var op = (string)Consume().Value!;
            var right = ParseComparison(depth + 1);
            left = new BinaryAstNode(op, left, right);
        }

        return left;
    }

    private AstNode ParseComparison(int depth)
    {
        GuardDepth(depth);
        var left = ParseAdditive(depth + 1);
        while ((Peek().Type == ExpressionTokenType.Punctuation && Peek().Value is "<" or ">") ||
               (Peek().Type == ExpressionTokenType.Operator && Peek().Value is "<=" or ">="))
        {
            var op = (string)Consume().Value!;
            var right = ParseAdditive(depth + 1);
            left = new BinaryAstNode(op, left, right);
        }

        return left;
    }

    private AstNode ParseAdditive(int depth)
    {
        GuardDepth(depth);
        var left = ParseMultiplicative(depth + 1);
        while (Peek().Type == ExpressionTokenType.Punctuation && Peek().Value is "+" or "-")
        {
            var op = (string)Consume().Value!;
            var right = ParseMultiplicative(depth + 1);
            left = new BinaryAstNode(op, left, right);
        }

        return left;
    }

    private AstNode ParseMultiplicative(int depth)
    {
        GuardDepth(depth);
        var left = ParseUnary(depth + 1);
        while (Peek().Type == ExpressionTokenType.Punctuation && Peek().Value is "*" or "/" or "%")
        {
            var op = (string)Consume().Value!;
            var right = ParseUnary(depth + 1);
            left = new BinaryAstNode(op, left, right);
        }

        return left;
    }

    private AstNode ParseUnary(int depth)
    {
        GuardDepth(depth);
        if (Peek().Type == ExpressionTokenType.Punctuation && Peek().Value is "!" or "-")
        {
            var op = (string)Consume().Value!;
            var argument = ParseUnary(depth + 1);
            return new UnaryAstNode(op, argument);
        }

        return ParseMember(depth + 1);
    }

    private AstNode ParseMember(int depth)
    {
        GuardDepth(depth);
        var node = ParsePrimary(depth + 1);
        while (IsPunctuation("."))
        {
            Consume();
            var propToken = Expect(ExpressionTokenType.Identifier);
            var propertyName = (string)propToken.Value!;
            if (ForbiddenProperties.Contains(propertyName))
            {
                throw new SafeExpressionException($"Access to forbidden property '{propertyName}' is blocked.");
            }

            node = new MemberAstNode(node, propertyName);
        }

        return node;
    }

    private AstNode ParsePrimary(int depth)
    {
        GuardDepth(depth);
        var token = Peek();

        if (token.Type is ExpressionTokenType.Number
            or ExpressionTokenType.String
            or ExpressionTokenType.Boolean
            or ExpressionTokenType.Null)
        {
            Consume();
            return new LiteralAstNode(token.Value);
        }

        if (token.Type == ExpressionTokenType.Identifier)
        {
            var name = (string)Consume().Value!;
            if (ForbiddenProperties.Contains(name))
            {
                throw new SafeExpressionException($"Identifier '{name}' is forbidden.");
            }

            if (IsPunctuation("("))
            {
                if (!WhitelistedFunctions.Contains(name))
                {
                    throw new SafeExpressionException($"Function '{name}' is not whitelisted.");
                }

                Consume();
                var args = new List<AstNode>();
                if (!IsPunctuation(")"))
                {
                    args.Add(ParseTernary(depth + 1));
                    while (IsPunctuation(","))
                    {
                        Consume();
                        args.Add(ParseTernary(depth + 1));
                    }
                }

                Expect(ExpressionTokenType.Punctuation, ")");
                return new CallAstNode(name, args);
            }

            return new IdentifierAstNode(name);
        }

        if (IsPunctuation("("))
        {
            Consume();
            var inner = ParseTernary(depth + 1);
            Expect(ExpressionTokenType.Punctuation, ")");
            return inner;
        }

        throw new SafeExpressionException($"Unexpected token '{token.Value ?? token.Type}'.");
    }

    private bool IsOperator(string op)
        => Peek().Type == ExpressionTokenType.Operator && string.Equals(Peek().Value as string, op, StringComparison.Ordinal);

    private bool IsPunctuation(string punct)
        => Peek().Type == ExpressionTokenType.Punctuation && string.Equals(Peek().Value as string, punct, StringComparison.Ordinal);

    private static void GuardDepth(int depth)
    {
        if (depth > MaxAstDepth)
        {
            throw new SafeExpressionException($"Expression exceeds max AST depth ({MaxAstDepth}).");
        }
    }
}
