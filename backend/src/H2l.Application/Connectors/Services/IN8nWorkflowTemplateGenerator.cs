using System.Text.Json;
using H2l.Application.Forms.Schema;
using H2l.Domain.Workflows;

namespace H2l.Application.Connectors.Services;

public interface IN8nWorkflowTemplateGenerator
{
    JsonElement Generate(
        FormDefinition formDefinition,
        FormVersion? latestPublishedVersion,
        FormSchemaDocument? parsedSchema,
        string variant,
        Guid? connectorId);
}
