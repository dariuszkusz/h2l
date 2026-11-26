using H2l.Application.Forms.Expressions;
using H2l.Application.Forms.Schema;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class FormSchemaValidatorTests
{
    private readonly FormSchemaValidator _sut = new(new SafeExpressionEvaluator());

    [Fact]
    public void ParseAndValidate_ValidSchemaWithRulesAndExpressions_Succeeds()
    {
        const string json = """
        {
          "schemaVersion": "1.0",
          "defaultLanguage": "en",
          "layoutMode": "wizard",
          "steps": [
            {
              "id": "step_1",
              "title": { "en": "Request", "de": "Antrag", "pl": "Wniosek" },
              "components": [
                {
                  "id": "cmp_cost",
                  "type": "number",
                  "variableKey": "estimatedCost",
                  "validators": [{ "type": "required" }, { "type": "min", "value": 1 }]
                },
                {
                  "id": "cmp_justification",
                  "type": "textarea",
                  "variableKey": "businessJustification",
                  "rules": {
                    "visibleIf": "estimatedCost > 1000",
                    "requiredIf": "estimatedCost > 1000"
                  },
                  "validators": [
                    {
                      "type": "expression",
                      "expression": "estimatedCost <= 1000 || (businessJustification && businessJustification.length >= 20)"
                    }
                  ]
                },
                {
                  "id": "cmp_iban",
                  "type": "text",
                  "variableKey": "vendorIban",
                  "isSensitive": true,
                  "validators": [{ "type": "regex", "pattern": "^[A-Z]{2}[0-9A-Z]{13,30}$" }]
                }
              ]
            }
          ]
        }
        """;

        var result = _sut.ParseAndValidate(json);
        Assert.True(result.IsSuccess);
        Assert.Equal("1.0", result.Value.SchemaVersion);
    }

    [Fact]
    public void ParseAndValidate_DuplicateVariableKey_ReturnsValidationFailure()
    {
        const string json = """
        {
          "schemaVersion": "1.0",
          "defaultLanguage": "en",
          "layoutMode": "single",
          "steps": [
            {
              "id": "step_1",
              "title": { "en": "Step" },
              "components": [
                { "id": "cmp_1", "type": "text", "variableKey": "duplicateKey" },
                { "id": "cmp_2", "type": "text", "variableKey": "duplicateKey" }
              ]
            }
          ]
        }
        """;

        var result = _sut.ParseAndValidate(json);
        Assert.True(result.IsFailure);
        Assert.Equal("FORM_VARIABLE_KEY_DUPLICATE", result.Error.Code);
    }

    [Fact]
    public void ParseAndValidate_InvalidVariableKeyRegex_ReturnsValidationFailure()
    {
        const string json = """
        {
          "schemaVersion": "1.0",
          "defaultLanguage": "en",
          "layoutMode": "single",
          "steps": [
            {
              "id": "step_1",
              "title": { "en": "Step" },
              "components": [
                { "id": "cmp_1", "type": "text", "variableKey": "123-invalid" }
              ]
            }
          ]
        }
        """;

        var result = _sut.ParseAndValidate(json);
        Assert.True(result.IsFailure);
        Assert.Equal("FORM_VARIABLE_KEY_INVALID", result.Error.Code);
    }

    [Fact]
    public void ParseAndValidate_ForbiddenAstExpressionInRule_ReturnsValidationFailure()
    {
        const string json = """
        {
          "schemaVersion": "1.0",
          "defaultLanguage": "en",
          "layoutMode": "single",
          "steps": [
            {
              "id": "step_1",
              "title": { "en": "Step" },
              "components": [
                {
                  "id": "cmp_1",
                  "type": "text",
                  "variableKey": "fieldA",
                  "rules": { "visibleIf": "fieldA.constructor" }
                }
              ]
            }
          ]
        }
        """;

        var result = _sut.ParseAndValidate(json);
        Assert.True(result.IsFailure);
        Assert.Equal("FORM_RULE_EXPRESSION_INVALID", result.Error.Code);
    }
}
