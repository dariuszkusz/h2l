using H2l.Application.Forms.Expressions;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class SafeExpressionEvaluatorTests
{
    private readonly SafeExpressionEvaluator _sut = new();

    [Fact]
    public void Evaluate_SupportsArithmeticComparisonsLogicalAndTernary()
    {
        var context = new Dictionary<string, object?>
        {
            ["estimatedCost"] = 1500d,
            ["discount"] = 200d,
            ["businessJustification"] = "Essential IDE license for backend team"
        };

        var netCost = _sut.Evaluate("estimatedCost - discount", context);
        Assert.True(netCost.IsSuccess);
        Assert.Equal(1300d, netCost.Value);

        var tier = _sut.Evaluate("estimatedCost > 1000 ? 'high' : 'low'", context);
        Assert.True(tier.IsSuccess);
        Assert.Equal("high", tier.Value);

        var rule = _sut.EvaluateBoolean(
            "estimatedCost <= 1000 || (businessJustification && businessJustification.length >= 20)",
            context);
        Assert.True(rule.IsSuccess);
        Assert.True(rule.Value);
    }

    [Fact]
    public void Evaluate_SupportsUnaryOperatorsAndWhitelistedFunctions()
    {
        var context = new Dictionary<string, object?>
        {
            ["approved"] = false,
            ["amount"] = 42d,
            ["role"] = "Admin",
            ["tags"] = new List<object?> { "urgent", "finance" }
        };

        Assert.Equal(true, _sut.Evaluate("!approved", context).Value);
        Assert.Equal(-42d, _sut.Evaluate("-amount", context).Value);
        Assert.Equal("admin", _sut.Evaluate("lower(role)", context).Value);
        Assert.Equal("ADMIN", _sut.Evaluate("upper(role)", context).Value);
        Assert.Equal(2d, _sut.Evaluate("len(tags)", context).Value);
        Assert.Equal(true, _sut.Evaluate("includes(tags, 'finance')", context).Value);
        Assert.Equal(true, _sut.Evaluate("isEmpty('   ')", context).Value);
    }

    [Fact]
    public void Evaluate_MemberAccessOnNestedDictionaryAndNullSafety()
    {
        var context = new Dictionary<string, object?>
        {
            ["vendor"] = new Dictionary<string, object?> { ["country"] = "DE" },
            ["missing"] = null
        };

        var country = _sut.Evaluate("vendor.country === 'DE'", context);
        Assert.True(country.IsSuccess);
        Assert.Equal(true, country.Value);

        var nullMember = _sut.Evaluate("missing.anything", context);
        Assert.True(nullMember.IsSuccess);
        Assert.Null(nullMember.Value);
    }

    [Theory]
    [InlineData("user.__proto__")]
    [InlineData("user.constructor")]
    [InlineData("user.prototype")]
    [InlineData("eval('1+1')")]
    [InlineData("alert(1)")]
    [InlineData("1 + * 2")]
    [InlineData("'unterminated")]
    public void ValidateSyntax_BlocksForbiddenPropertiesNonWhitelistedCallsAndInvalidSyntax(string expression)
    {
        var result = _sut.ValidateSyntax(expression);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ValidateSyntax_EnforcesMaxLengthAndMaxAstDepth()
    {
        var tooLong = new string('a', ExpressionTokenizer.MaxExpressionLength + 1);
        Assert.True(_sut.ValidateSyntax(tooLong).IsFailure);

        var deep = string.Concat(Enumerable.Repeat("(", 40)) + "1" + string.Concat(Enumerable.Repeat(")", 40));
        Assert.True(_sut.ValidateSyntax(deep).IsFailure);
    }

    [Fact]
    public void Evaluate_DivisionByZeroProducesNaNAndComparisonsWithNaNReturnFalse()
    {
        var context = new Dictionary<string, object?> { ["x"] = 10d, ["y"] = 0d };

        var div = _sut.Evaluate("x / y", context);
        Assert.True(div.IsSuccess);
        Assert.True(double.IsNaN((double)div.Value!));

        Assert.Equal(false, _sut.Evaluate("(x / y) < 5", context).Value);
        Assert.Equal(false, _sut.Evaluate("(x / y) > 5", context).Value);
    }
}
