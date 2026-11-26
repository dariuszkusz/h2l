using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using H2l.Application.Forms.Expressions;
using H2l.Domain.Common;

namespace H2l.Application.Forms.Schema;

public sealed partial class FormSubmissionValidator : IFormSubmissionValidator
{
    private readonly ISafeExpressionEvaluator _expressionEvaluator;

    public FormSubmissionValidator(ISafeExpressionEvaluator expressionEvaluator)
    {
        _expressionEvaluator = expressionEvaluator;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public Result<ValidatedSubmissionPayload> ValidateAndSplit(
        FormSchemaDocument schema,
        IReadOnlyDictionary<string, JsonElement> submittedValues,
        IReadOnlyDictionary<string, JsonElement>? prefilledValues = null,
        IReadOnlyCollection<string>? uploadedAttachmentFieldKeys = null)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(submittedValues);

        var effectiveValues = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (prefilledValues is not null)
        {
            foreach (var kvp in prefilledValues)
            {
                effectiveValues[kvp.Key] = kvp.Value;
            }
        }

        foreach (var kvp in submittedValues)
        {
            effectiveValues[kvp.Key] = kvp.Value;
        }

        var attachmentKeySet = uploadedAttachmentFieldKeys is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(uploadedAttachmentFieldKeys, StringComparer.Ordinal);

        var evalContext = BuildEvaluationContext(effectiveValues);
        var publicValues = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var sensitiveValues = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var step in schema.Steps)
        {
            var stepResult = ValidateComponents(
                step.Components,
                effectiveValues,
                evalContext,
                attachmentKeySet,
                publicValues,
                sensitiveValues);

            if (stepResult.IsFailure)
            {
                return Result.Failure<ValidatedSubmissionPayload>(stepResult.Error);
            }
        }

        return Result.Success(new ValidatedSubmissionPayload(publicValues, sensitiveValues));
    }

    public FormComponentSchema? FindComponentByVariableKey(
        FormSchemaDocument schema,
        string variableKey)
    {
        foreach (var step in schema.Steps)
        {
            var found = FindInComponents(step.Components, variableKey);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static FormComponentSchema? FindInComponents(
        IReadOnlyList<FormComponentSchema> components,
        string variableKey)
    {
        foreach (var component in components)
        {
            if (string.Equals(component.VariableKey, variableKey, StringComparison.Ordinal))
            {
                return component;
            }

            if (component.Components is { Count: > 0 })
            {
                var nested = FindInComponents(component.Components, variableKey);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private Result ValidateComponents(
        IReadOnlyList<FormComponentSchema> components,
        IReadOnlyDictionary<string, JsonElement> effectiveValues,
        IReadOnlyDictionary<string, object?> evalContext,
        HashSet<string> attachmentKeySet,
        Dictionary<string, JsonElement> publicValues,
        Dictionary<string, JsonElement> sensitiveValues)
    {
        foreach (var component in components)
        {
            if (!string.IsNullOrWhiteSpace(component.Rules?.VisibleIf))
            {
                var visibleResult = _expressionEvaluator.EvaluateBoolean(component.Rules.VisibleIf, evalContext);
                if (visibleResult.IsFailure)
                {
                    return Result.Failure(Error.Validation(
                        "FORM_RULE_EVALUATION_FAILED",
                        $"Failed evaluating visibleIf for '{component.Id}': {visibleResult.Error.Message}"));
                }

                if (!visibleResult.Value)
                {
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(component.VariableKey))
            {
                var variableKey = component.VariableKey;
                var hasValue = effectiveValues.TryGetValue(variableKey, out var rawValue);
                var hasAttachment = string.Equals(component.Type, "file", StringComparison.Ordinal)
                    && attachmentKeySet.Contains(variableKey);

                var isRequired = false;
                if (component.Validators is not null &&
                    component.Validators.Any(v => string.Equals(v.Type, "required", StringComparison.Ordinal)))
                {
                    isRequired = true;
                }

                if (!isRequired && !string.IsNullOrWhiteSpace(component.Rules?.RequiredIf))
                {
                    var requiredResult = _expressionEvaluator.EvaluateBoolean(component.Rules.RequiredIf, evalContext);
                    if (requiredResult.IsFailure)
                    {
                        return Result.Failure(Error.Validation(
                            "FORM_RULE_EVALUATION_FAILED",
                            $"Failed evaluating requiredIf for '{component.Id}': {requiredResult.Error.Message}"));
                    }

                    isRequired = requiredResult.Value;
                }

                if (isRequired && !hasAttachment && (!hasValue || IsMissingValue(rawValue)))
                {
                    return Result.Failure(Error.Validation(
                        "FORM_FIELD_REQUIRED",
                        $"Field '{variableKey}' is required."));
                }

                if (component.Validators is { Count: > 0 })
                {
                    foreach (var validator in component.Validators)
                    {
                        var validationResult = EvaluateValidator(
                            component,
                            validator,
                            hasValue ? rawValue : null,
                            hasAttachment,
                            evalContext);

                        if (validationResult.IsFailure)
                        {
                            return validationResult;
                        }
                    }
                }

                if (hasValue && rawValue.ValueKind != JsonValueKind.Undefined)
                {
                    if (component.IsSensitive == true)
                    {
                        sensitiveValues[variableKey] = rawValue.Clone();
                    }
                    else
                    {
                        publicValues[variableKey] = rawValue.Clone();
                    }
                }
            }

            if (component.Components is { Count: > 0 })
            {
                var nestedResult = ValidateComponents(
                    component.Components,
                    effectiveValues,
                    evalContext,
                    attachmentKeySet,
                    publicValues,
                    sensitiveValues);

                if (nestedResult.IsFailure)
                {
                    return nestedResult;
                }
            }
        }

        return Result.Success();
    }

    private Result EvaluateValidator(
        FormComponentSchema component,
        FormFieldValidatorSchema validator,
        JsonElement? value,
        bool hasAttachment,
        IReadOnlyDictionary<string, object?> evalContext)
    {
        var key = component.VariableKey ?? component.Id;

        switch (validator.Type)
        {
            case "required":
                if (!hasAttachment && (!value.HasValue || IsMissingValue(value.Value)))
                {
                    return Result.Failure(Error.Validation("FORM_FIELD_REQUIRED", $"Field '{key}' is required."));
                }

                return Result.Success();

            case "expression":
                if (string.IsNullOrWhiteSpace(validator.Expression))
                {
                    return Result.Success();
                }

                var exprResult = _expressionEvaluator.EvaluateBoolean(validator.Expression, evalContext);
                if (exprResult.IsFailure)
                {
                    return Result.Failure(Error.Validation(
                        "FORM_EXPRESSION_VALIDATION_ERROR",
                        $"Expression validation failed for '{key}': {exprResult.Error.Message}"));
                }

                return exprResult.Value
                    ? Result.Success()
                    : Result.Failure(Error.Validation(
                        "FORM_EXPRESSION_VALIDATION_FAILED",
                        $"Field '{key}' failed expression validation."));
        }

        if (!value.HasValue || IsMissingValue(value.Value))
        {
            return Result.Success();
        }

        var element = value.Value;

        switch (validator.Type)
        {
            case "min":
                if (TryGetNumeric(element, out var numForMin) &&
                    validator.Value.HasValue &&
                    TryGetNumeric(validator.Value.Value, out var minLimit) &&
                    numForMin < minLimit)
                {
                    return Result.Failure(Error.Validation(
                        "FORM_FIELD_MIN",
                        $"Field '{key}' must be greater than or equal to {minLimit}."));
                }

                return Result.Success();

            case "max":
                if (TryGetNumeric(element, out var numForMax) &&
                    validator.Value.HasValue &&
                    TryGetNumeric(validator.Value.Value, out var maxLimit) &&
                    numForMax > maxLimit)
                {
                    return Result.Failure(Error.Validation(
                        "FORM_FIELD_MAX",
                        $"Field '{key}' must be less than or equal to {maxLimit}."));
                }

                return Result.Success();

            case "minLength":
                if (validator.Value.HasValue &&
                    TryGetNumeric(validator.Value.Value, out var minLen) &&
                    TryGetLength(element, out var actualMinLen) &&
                    actualMinLen < minLen)
                {
                    return Result.Failure(Error.Validation(
                        "FORM_FIELD_MIN_LENGTH",
                        $"Field '{key}' length must be at least {minLen}."));
                }

                return Result.Success();

            case "maxLength":
                if (validator.Value.HasValue &&
                    TryGetNumeric(validator.Value.Value, out var maxLen) &&
                    TryGetLength(element, out var actualMaxLen) &&
                    actualMaxLen > maxLen)
                {
                    return Result.Failure(Error.Validation(
                        "FORM_FIELD_MAX_LENGTH",
                        $"Field '{key}' length must be at most {maxLen}."));
                }

                return Result.Success();

            case "email":
                if (element.ValueKind == JsonValueKind.String)
                {
                    var email = element.GetString() ?? string.Empty;
                    if (!EmailRegex().IsMatch(email))
                    {
                        return Result.Failure(Error.Validation(
                            "FORM_FIELD_EMAIL_INVALID",
                            $"Field '{key}' must be a valid email address."));
                    }
                }

                return Result.Success();

            case "regex":
                if (!string.IsNullOrWhiteSpace(validator.Pattern) && element.ValueKind == JsonValueKind.String)
                {
                    var text = element.GetString() ?? string.Empty;
                    try
                    {
                        if (!Regex.IsMatch(text, validator.Pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                        {
                            return Result.Failure(Error.Validation(
                                "FORM_FIELD_REGEX_MISMATCH",
                                $"Field '{key}' does not match the required pattern."));
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        return Result.Failure(Error.Validation(
                            "FORM_FIELD_REGEX_TIMEOUT",
                            $"Field '{key}' regex validation timed out."));
                    }
                }

                return Result.Success();

            case "compareField":
                return EvaluateCompareField(key, element, validator, evalContext);

            default:
                return Result.Success();
        }
    }

    private static Result EvaluateCompareField(
        string key,
        JsonElement currentElement,
        FormFieldValidatorSchema validator,
        IReadOnlyDictionary<string, object?> evalContext)
    {
        if (string.IsNullOrWhiteSpace(validator.TargetField) || string.IsNullOrWhiteSpace(validator.Operator))
        {
            return Result.Success();
        }

        evalContext.TryGetValue(validator.TargetField, out var targetValue);
        var currentValue = ConvertJsonElement(currentElement);

        var passed = validator.Operator switch
        {
            "eq" => CompareValues(currentValue, targetValue) == 0,
            "neq" => CompareValues(currentValue, targetValue) != 0,
            "gt" => CompareValues(currentValue, targetValue) > 0,
            "gte" => CompareValues(currentValue, targetValue) >= 0,
            "lt" => CompareValues(currentValue, targetValue) < 0,
            "lte" => CompareValues(currentValue, targetValue) <= 0,
            _ => true
        };

        return passed
            ? Result.Success()
            : Result.Failure(Error.Validation(
                "FORM_FIELD_COMPARE_FAILED",
                $"Field '{key}' failed comparison '{validator.Operator}' against '{validator.TargetField}'."));
    }

    private static int CompareValues(object? left, object? right)
    {
        if (left is null && right is null)
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        if (left is double ld && right is double rd)
        {
            return ld.CompareTo(rd);
        }

        if (left is string ls && right is string rs)
        {
            return string.CompareOrdinal(ls, rs);
        }

        if (left is bool lb && right is bool rb)
        {
            return lb.CompareTo(rb);
        }

        return string.CompareOrdinal(
            Convert.ToString(left, CultureInfo.InvariantCulture),
            Convert.ToString(right, CultureInfo.InvariantCulture));
    }

    private static bool IsMissingValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => true,
        JsonValueKind.String => string.IsNullOrWhiteSpace(element.GetString()),
        JsonValueKind.Array => element.GetArrayLength() == 0,
        _ => false
    };

    private static bool TryGetNumeric(JsonElement element, out double value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryGetLength(JsonElement element, out int length)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            length = (element.GetString() ?? string.Empty).Length;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            length = element.GetArrayLength();
            return true;
        }

        length = 0;
        return false;
    }

    private static Dictionary<string, object?> BuildEvaluationContext(
        IReadOnlyDictionary<string, JsonElement> values)
    {
        var context = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in values)
        {
            context[kvp.Key] = ConvertJsonElement(kvp.Value);
        }

        return context;
    }

    private static object? ConvertJsonElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(
            p => p.Name,
            p => ConvertJsonElement(p.Value),
            StringComparer.Ordinal),
        _ => null
    };
}
