using System.Text.Json;
using H2l.Domain.Common;

namespace H2l.Application.Forms.Schema;

public interface IFormSubmissionValidator
{
    Result<ValidatedSubmissionPayload> ValidateAndSplit(
        FormSchemaDocument schema,
        IReadOnlyDictionary<string, JsonElement> submittedValues,
        IReadOnlyDictionary<string, JsonElement>? prefilledValues = null,
        IReadOnlyCollection<string>? uploadedAttachmentFieldKeys = null);

    FormComponentSchema? FindComponentByVariableKey(
        FormSchemaDocument schema,
        string variableKey);
}
