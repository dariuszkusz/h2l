using System.Globalization;
using System.Text;

namespace H2l.Application.Forms.Expressions;

public static class ExpressionTokenizer
{
    public const int MaxExpressionLength = 1024;

    private const string SingleCharPunctuation = "().,?:+-*/%<>!";

    public static IReadOnlyList<ExpressionToken> Tokenize(string source)
    {
        if (source is null)
        {
            throw new SafeExpressionException("Expression must be a non-null string.");
        }

        if (source.Length > MaxExpressionLength)
        {
            throw new SafeExpressionException($"Expression exceeds max length ({MaxExpressionLength}).");
        }

        var tokens = new List<ExpressionToken>();
        var i = 0;

        while (i < source.Length)
        {
            var ch = source[i];

            if (char.IsWhiteSpace(ch))
            {
                i++;
                continue;
            }

            if (i + 3 <= source.Length)
            {
                var three = source.Substring(i, 3);
                if (three is "===" or "!==")
                {
                    tokens.Add(new ExpressionToken(ExpressionTokenType.Operator, three));
                    i += 3;
                    continue;
                }
            }

            if (i + 2 <= source.Length)
            {
                var two = source.Substring(i, 2);
                if (two is "<=" or ">=" or "==" or "!=" or "&&" or "||")
                {
                    tokens.Add(new ExpressionToken(ExpressionTokenType.Operator, two));
                    i += 2;
                    continue;
                }
            }

            if (SingleCharPunctuation.Contains(ch, StringComparison.Ordinal))
            {
                tokens.Add(new ExpressionToken(ExpressionTokenType.Punctuation, ch.ToString()));
                i++;
                continue;
            }

            if (ch is '\'' or '"')
            {
                var quote = ch;
                var sb = new StringBuilder();
                i++;
                var closed = false;

                while (i < source.Length)
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                    {
                        sb.Append(source[i + 1]);
                        i += 2;
                        continue;
                    }

                    if (source[i] == quote)
                    {
                        closed = true;
                        i++;
                        break;
                    }

                    sb.Append(source[i]);
                    i++;
                }

                if (!closed)
                {
                    throw new SafeExpressionException("Unterminated string literal.");
                }

                tokens.Add(new ExpressionToken(ExpressionTokenType.String, sb.ToString()));
                continue;
            }

            if (ch is >= '0' and <= '9')
            {
                var start = i;
                var hasDot = false;
                while (i < source.Length && ((source[i] is >= '0' and <= '9') || (source[i] == '.' && !hasDot)))
                {
                    if (source[i] == '.')
                    {
                        hasDot = true;
                    }

                    i++;
                }

                var numStr = source[start..i];
                if (!double.TryParse(numStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
                {
                    throw new SafeExpressionException($"Invalid number literal '{numStr}'.");
                }

                tokens.Add(new ExpressionToken(ExpressionTokenType.Number, parsed));
                continue;
            }

            if (IsIdentifierStart(ch))
            {
                var start = i;
                while (i < source.Length && IsIdentifierPart(source[i]))
                {
                    i++;
                }

                var ident = source[start..i];
                if (ident is "true" or "false")
                {
                    tokens.Add(new ExpressionToken(ExpressionTokenType.Boolean, ident == "true"));
                }
                else if (ident == "null")
                {
                    tokens.Add(new ExpressionToken(ExpressionTokenType.Null, null));
                }
                else
                {
                    tokens.Add(new ExpressionToken(ExpressionTokenType.Identifier, ident));
                }

                continue;
            }

            throw new SafeExpressionException($"Unexpected character '{ch}' at position {i}.");
        }

        tokens.Add(new ExpressionToken(ExpressionTokenType.EndOfFile, null));
        return tokens;
    }

    private static bool IsIdentifierStart(char ch)
        => (ch is >= 'a' and <= 'z') || (ch is >= 'A' and <= 'Z') || ch is '_' or '$';

    private static bool IsIdentifierPart(char ch)
        => IsIdentifierStart(ch) || (ch is >= '0' and <= '9');
}
