using System.Text.Json;
using H2l.Application.Forms.Expressions;
using H2l.Application.Forms.Schema;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class FormSubmissionValidatorTests
{
    private readonly FormSchemaValidator _schemaValidator;
    private readonly FormSubmissionValidator _sut;

    public FormSubmissionValidatorTests()
    {
        var evaluator = new SafeExpressionEvaluator();
        _schemaValidator = new FormSchemaValidator(evaluator);
        _sut = new FormSubmissionValidator(evaluator);
    }

    [Fact]
    public void ValidateAndSplit_SplitsSensitiveAndPublicFieldsAndEvaluatesConditionalRules()
    {
        const string schemaJson = """
        {
          "schemaVersion": "1.0",
          "defaultLanguage": "en",
          "layoutMode": "single",
          "steps": [
            {
              "id": "step_1",
              "title": { "en": "Purchase" },
              "components": [
                {
                  "id": "cmp_cost",
                  "type": "number",
                  "variableKey": "estimatedCost",
                  "validators": [{ "type": "required" }, { "type": "min", "value": 1 }]
                },
                {
                  "id": "cmp_just",
                  "type": "textarea",
                  "variableKey": "businessJustification",
                  "rules": {
                    "visibleIf": "estimatedCost > 1000",
                    "requiredIf": "estimatedCost > 1000"
                  },
                  "validators": [
                    {
                      "type": "expression",
                      "expression": "estimatedCost <= 1000 || (businessJustification && businessJustification.length >= 10)"
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

        var schema = _schemaValidator.ParseAndValidate(schemaJson).Value;
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
        {
          "estimatedCost": 2500,
          "businessJustification": "Needed for quarterly compliance audit",
          "vendorIban": "DE89370400440532013000"
        }
        """)!;

        var result = _sut.ValidateAndSplit(schema, values);

        Assert.True(result.IsSuccess);
        Assert.Contains("estimatedCost", result.Value.PublicValues.Keys);
        Assert.Contains("businessJustification", result.Value.PublicValues.Keys);
        Assert.DoesNotContain("vendorIban", result.Value.PublicValues.Keys);
        Assert.Contains("vendorIban", result.Value.SensitiveValues.Keys);
        Assert.Equal("DE89370400440532013000", result.Value.SensitiveValues["vendorIban"].GetString());
    }

    [Fact]
    public void ValidateAndSplit_WhenVisibleIfIsFalse_SkipsHiddenFieldValidation()
    {
        const string schemaJson = """
        {
          "schemaVersion": "1.0",
          "defaultLanguage": "en",
          "layoutMode": "single",
          "steps": [
            {
              "id": "step_1",
              "title": { "en": "Purchase" },
              "components": [
                {
                  "id": "cmp_cost",
                  "type": "number",
                  "variableKey": "estimatedCost",
                  "validators": [{ "type": "required" }]
                },
                {
                  "id": "cmp_just",
                  "type": "textarea",
                  "variableKey": "businessJustification",
                  "rules": {
                    "visibleIf": "estimatedCost > 1000",
                    "requiredIf": "estimatedCost > 1000"
                  },
                  "validators": [{ "type": "minLength", "value": 20 }]
                }
              ]
            }
          ]
        }
        """;

        var schema = _schemaValidator.ParseAndValidate(schemaJson).Value;
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
        {
          "estimatedCost": 500
        }
        """)!;

        var result = _sut.ValidateAndSplit(schema, values);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateAndSplit_ValidatesEmailAndCompareField()
    {
        const string schemaJson = """
        {
          "schemaVersion": "1.0",
          "defaultLanguage": "en",
          "layoutMode": "single",
          "steps": [
            {
              "id": "step_1",
              "title": { "en": "Range" },
              "components": [
                {
                  "id": "cmp_email",
                  "type": "email",
                  "variableKey": "contactEmail",
                  "validators": [{ "type": "email" }]
                },
                {
                  "id": "cmp_min",
                  "type": "number",
                  "variableKey": "minBudget"
                },
                {
                  "id": "cmp_max",
                  "type": "number",
                  "variableKey": "maxBudget",
                  "validators": [{ "type": "compareField", "targetField": "minBudget", "operator": "gte" }]
                }
              ]
            }
          ]
        }
        """;

        var schema = _schemaValidator.ParseAndValidate(schemaJson).Value;

        var invalidEmail = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
        { "contactEmail": "not-an-email", "minBudget": 100, "maxBudget": 200 }
        """)!;
        Assert.Equal("FORM_FIELD_EMAIL_INVALID", _sut.ValidateAndSplit(schema, invalidEmail).Error.Code);

        var invalidCompare = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
        { "contactEmail": "ok@example.com", "minBudget": 300, "maxBudget": 200 }
        """)!;
        Assert.Equal("FORM_FIELD_COMPARE_FAILED", _sut.ValidateAndSplit(schema, invalidCompare).Error.Code);
    }
}
