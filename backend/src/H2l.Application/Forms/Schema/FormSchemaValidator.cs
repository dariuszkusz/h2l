using System.Text.Json;
using System.Text.RegularExpressions;
using H2l.Application.Forms.Expressions;
using H2l.Domain.Common;

namespace H2l.Application.Forms.Schema;

public sealed partial class FormSchemaValidator : IFormSchemaValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlySet<string> SupportedLanguages = new HashSet<string>(StringComparer.Ordinal)
    {
        "en",
        "de",
        "pl"
    };

    private static readonly IReadOnlySet<string> SupportedLayoutModes = new HashSet<string>(StringComparer.Ordinal)
    {
        "single",
        "wizard"
    };

    private static readonly IReadOnlySet<string> SupportedComponentTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "text",
        "textarea",
        "number",
        "date",
        "time",
        "datetime",
        "email",
        "phone",
        "url",
        "select",
        "radio",
        "checkbox",
        "toggle",
        "file",
        "section",
        "grid",
        "repeater",
        "heading",
        "divider"
    };

    private static readonly IReadOnlySet<string> NonDataComponentTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "section",
        "grid",
        "heading",
        "divider"
    };

    private static readonly IReadOnlySet<string> SupportedValidatorTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "required",
        "min",
        "max",
        "minLength",
        "maxLength",
        "email",
        "regex",
        "compareField",
        "expression"
    };

    private static readonly IReadOnlySet<string> SupportedCompareOperators = new HashSet<string>(StringComparer.Ordinal)
    {
        "eq",
        "neq",
        "gt",
        "gte",
        "lt",
        "lte"
    };

    private readonly ISafeExpressionEvaluator _expressionEvaluator;

    public FormSchemaValidator(ISafeExpressionEvaluator expressionEvaluator)
    {
        _expressionEvaluator = expressionEvaluator;
    }

    [GeneratedRegex("^[a-zA-Z][a-zA-Z0-9_]{0,63}$", RegexOptions.Compiled)]
    private static partial Regex VariableKeyRegex();

    public Result<FormSchemaDocument> ParseAndValidate(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            return Result.Failure<FormSchemaDocument>(
                Error.Validation("FORM_SCHEMA_REQUIRED", "Form schema JSON is required."));
        }

        try
        {
            var document = JsonSerializer.Deserialize<FormSchemaDocument>(schemaJson, SerializerOptions);
            if (document is null)
            {
                return Result.Failure<FormSchemaDocument>(
                    Error.Validation("FORM_SCHEMA_INVALID", "Form schema JSON could not be deserialized."));
            }

            var validationResult = Validate(document);
            return validationResult.IsFailure
                ? Result.Failure<FormSchemaDocument>(validationResult.Error)
                : Result.Success(document);
        }
        catch (JsonException ex)
        {
            return Result.Failure<FormSchemaDocument>(
                Error.Validation("FORM_SCHEMA_JSON_INVALID", $"Form schema JSON is malformed: {ex.Message}"));
        }
    }

    public Result<FormSchemaDocument> ParseAndValidate(JsonElement schemaElement)
    {
        if (schemaElement.ValueKind != JsonValueKind.Object)
        {
            return Result.Failure<FormSchemaDocument>(
                Error.Validation("FORM_SCHEMA_INVALID", "Form schema must be a JSON object."));
        }

        return ParseAndValidate(schemaElement.GetRawText());
    }

    public Result Validate(FormSchemaDocument schema)
    {
        if (schema is null)
        {
            return Result.Failure(Error.Validation("FORM_SCHEMA_REQUIRED", "Form schema is required."));
        }

        if (!string.Equals(schema.SchemaVersion, "1.0", StringComparison.Ordinal))
        {
            return Result.Failure(Error.Validation(
                "FORM_SCHEMA_VERSION_UNSUPPORTED",
                "Supported schemaVersion is '1.0'."));
        }

        if (string.IsNullOrWhiteSpace(schema.DefaultLanguage) || !SupportedLanguages.Contains(schema.DefaultLanguage))
        {
            return Result.Failure(Error.Validation(
                "FORM_SCHEMA_DEFAULT_LANGUAGE_INVALID",
                "defaultLanguage must be one of: en, de, pl."));
        }

        if (string.IsNullOrWhiteSpace(schema.LayoutMode) || !SupportedLayoutModes.Contains(schema.LayoutMode))
        {
            return Result.Failure(Error.Validation(
                "FORM_SCHEMA_LAYOUT_MODE_INVALID",
                "layoutMode must be one of: single, wizard."));
        }

        if (schema.Steps is null || schema.Steps.Count == 0)
        {
            return Result.Failure(Error.Validation(
                "FORM_SCHEMA_STEPS_REQUIRED",
                "Form schema must contain at least one step."));
        }

        var stepIds = new HashSet<string>(StringComparer.Ordinal);
        var componentIds = new HashSet<string>(StringComparer.Ordinal);
        var variableKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in schema.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
            {
                return Result.Failure(Error.Validation("FORM_STEP_ID_REQUIRED", "Each form step must have a non-empty id."));
            }

            if (!stepIds.Add(step.Id))
            {
                return Result.Failure(Error.Validation("FORM_STEP_ID_DUPLICATE", $"Duplicate step id '{step.Id}'."));
            }

            if (step.Components is null)
            {
                return Result.Failure(Error.Validation("FORM_STEP_COMPONENTS_REQUIRED", $"Step '{step.Id}' must define a components array."));
            }

            var componentsResult = ValidateComponents(step.Components, componentIds, variableKeys, 0);
            if (componentsResult.IsFailure)
            {
                return componentsResult;
            }
        }

        return Result.Success();
    }

    private Result ValidateComponents(
        IReadOnlyList<FormComponentSchema> components,
        HashSet<string> componentIds,
        HashSet<string> variableKeys,
        int nestingDepth)
    {
        if (nestingDepth > 16)
        {
            return Result.Failure(Error.Validation(
                "FORM_COMPONENT_NESTING_TOO_DEEP",
                "Form component nesting depth exceeds maximum allowed limit."));
        }

        foreach (var component in components)
        {
            if (string.IsNullOrWhiteSpace(component.Id))
            {
                return Result.Failure(Error.Validation("FORM_COMPONENT_ID_REQUIRED", "Each component must have a non-empty id."));
            }

            if (!componentIds.Add(component.Id))
            {
                return Result.Failure(Error.Validation("FORM_COMPONENT_ID_DUPLICATE", $"Duplicate component id '{component.Id}'."));
            }

            if (string.IsNullOrWhiteSpace(component.Type) || !SupportedComponentTypes.Contains(component.Type))
            {
                return Result.Failure(Error.Validation(
                    "FORM_COMPONENT_TYPE_INVALID",
                    $"Component '{component.Id}' has unsupported type '{component.Type}'."));
            }

            var isDataComponent = !NonDataComponentTypes.Contains(component.Type);
            if (isDataComponent)
            {
                if (string.IsNullOrWhiteSpace(component.VariableKey))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_VARIABLE_KEY_REQUIRED",
                        $"Component '{component.Id}' of type '{component.Type}' requires a variableKey."));
                }

                if (!VariableKeyRegex().IsMatch(component.VariableKey))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_VARIABLE_KEY_INVALID",
                        $"Component '{component.Id}' has invalid variableKey '{component.VariableKey}'. Must match ^[a-zA-Z][a-zA-Z0-9_]{{0,63}}$."));
                }

                if (!variableKeys.Add(component.VariableKey))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_VARIABLE_KEY_DUPLICATE",
                        $"Duplicate variableKey '{component.VariableKey}' found in form schema."));
                }
            }
            else if (!string.IsNullOrWhiteSpace(component.VariableKey))
            {
                if (!VariableKeyRegex().IsMatch(component.VariableKey))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_VARIABLE_KEY_INVALID",
                        $"Component '{component.Id}' has invalid variableKey '{component.VariableKey}'."));
                }

                if (!variableKeys.Add(component.VariableKey))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_VARIABLE_KEY_DUPLICATE",
                        $"Duplicate variableKey '{component.VariableKey}' found in form schema."));
                }
            }

            var rulesResult = ValidateRules(component);
            if (rulesResult.IsFailure)
            {
                return rulesResult;
            }

            var validatorsResult = ValidateValidators(component);
            if (validatorsResult.IsFailure)
            {
                return validatorsResult;
            }

            if (component.Components is { Count: > 0 })
            {
                var nestedResult = ValidateComponents(component.Components, componentIds, variableKeys, nestingDepth + 1);
                if (nestedResult.IsFailure)
                {
                    return nestedResult;
                }
            }
        }

        return Result.Success();
    }

    private Result ValidateRules(FormComponentSchema component)
    {
        if (component.Rules is null)
        {
            return Result.Success();
        }

        var ruleExpressions = new (string RuleName, string? Expression)[]
        {
            ("visibleIf", component.Rules.VisibleIf),
            ("requiredIf", component.Rules.RequiredIf),
            ("readOnlyIf", component.Rules.ReadOnlyIf)
        };

        foreach (var (ruleName, expression) in ruleExpressions)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                continue;
            }

            var syntaxResult = _expressionEvaluator.ValidateSyntax(expression);
            if (syntaxResult.IsFailure)
            {
                return Result.Failure(Error.Validation(
                    "FORM_RULE_EXPRESSION_INVALID",
                    $"Component '{component.Id}' has invalid '{ruleName}' expression: {syntaxResult.Error.Message}"));
            }
        }

        return Result.Success();
    }

    private Result ValidateValidators(FormComponentSchema component)
    {
        if (component.Validators is null || component.Validators.Count == 0)
        {
            return Result.Success();
        }

        foreach (var validator in component.Validators)
        {
            if (string.IsNullOrWhiteSpace(validator.Type) || !SupportedValidatorTypes.Contains(validator.Type))
            {
                return Result.Failure(Error.Validation(
                    "FORM_VALIDATOR_TYPE_INVALID",
                    $"Component '{component.Id}' has unsupported validator type '{validator.Type}'."));
            }

            if (string.Equals(validator.Type, "expression", StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(validator.Expression))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_EXPRESSION_VALIDATOR_EMPTY",
                        $"Component '{component.Id}' expression validator requires a non-empty expression."));
                }

                var syntaxResult = _expressionEvaluator.ValidateSyntax(validator.Expression);
                if (syntaxResult.IsFailure)
                {
                    return Result.Failure(Error.Validation(
                        "FORM_VALIDATOR_EXPRESSION_INVALID",
                        $"Component '{component.Id}' has invalid validator expression: {syntaxResult.Error.Message}"));
                }
            }
            else if (string.Equals(validator.Type, "regex", StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(validator.Pattern))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_REGEX_VALIDATOR_EMPTY",
                        $"Component '{component.Id}' regex validator requires a non-empty pattern."));
                }

                try
                {
                    _ = new Regex(validator.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
                }
                catch (ArgumentException ex)
                {
                    return Result.Failure(Error.Validation(
                        "FORM_REGEX_VALIDATOR_INVALID",
                        $"Component '{component.Id}' has invalid regex pattern: {ex.Message}"));
                }
            }
            else if (string.Equals(validator.Type, "compareField", StringComparison.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(validator.TargetField))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_COMPARE_FIELD_TARGET_REQUIRED",
                        $"Component '{component.Id}' compareField validator requires targetField."));
                }

                if (string.IsNullOrWhiteSpace(validator.Operator) || !SupportedCompareOperators.Contains(validator.Operator))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_COMPARE_FIELD_OPERATOR_INVALID",
                        $"Component '{component.Id}' compareField validator operator must be one of: eq, neq, gt, gte, lt, lte."));
                }
            }
        }

        return Result.Success();
    }
}
