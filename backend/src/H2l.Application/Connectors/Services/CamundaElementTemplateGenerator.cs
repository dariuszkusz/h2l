using System.Text.Json;
using H2l.Application.Forms.Schema;
using H2l.Domain.Workflows;

namespace H2l.Application.Connectors.Services;

public sealed class CamundaElementTemplateGenerator : ICamundaElementTemplateGenerator
{
    private const string SchemaUrl =
        "https://unpkg.com/@camunda/zeebe-element-templates-json-schema/resources/schema.json";

    public JsonElement Generate(
        FormDefinition formDefinition,
        FormVersion? latestPublishedVersion,
        FormSchemaDocument? parsedSchema,
        bool generic)
    {
        var versionNumber = latestPublishedVersion?.VersionNumber;
        var variableKeys = ExtractVariableKeys(parsedSchema);
        var variableHint = variableKeys.Count > 0
            ? $"Form output variables: {string.Join(", ", variableKeys)}."
            : "Configure a form version to inspect variable keys.";

        var properties = new List<object>
        {
            new Dictionary<string, object?>
            {
                ["type"] = "Hidden",
                ["value"] = "h2l-human-task",
                ["binding"] = new Dictionary<string, object?>
                {
                    ["type"] = "zeebe:taskDefinition",
                    ["property"] = "type"
                }
            },
            new Dictionary<string, object?>
            {
                ["label"] = "Form ID",
                ["description"] = "h2l FormDefinition UUID.",
                ["type"] = "String",
                ["value"] = generic ? string.Empty : formDefinition.Id.ToString(),
                ["feel"] = "optional",
                ["binding"] = new Dictionary<string, object?>
                {
                    ["type"] = "zeebe:taskHeader",
                    ["key"] = "formId"
                },
                ["constraints"] = new Dictionary<string, object?>
                {
                    ["notEmpty"] = true
                }
            },
            new Dictionary<string, object?>
            {
                ["label"] = "Form Version",
                ["description"] = "Optional pinned FormVersion number. Leave empty to use latest published version.",
                ["type"] = "String",
                ["value"] = generic || !versionNumber.HasValue ? string.Empty : versionNumber.Value.ToString(),
                ["feel"] = "optional",
                ["binding"] = new Dictionary<string, object?>
                {
                    ["type"] = "zeebe:taskHeader",
                    ["key"] = "formVersion"
                }
            },
            new Dictionary<string, object?>
            {
                ["label"] = "Assignee",
                ["description"] = "Target user email or externalId (supports FEEL expressions, e.g. =approverEmail).",
                ["type"] = "String",
                ["value"] = string.Empty,
                ["feel"] = "optional",
                ["binding"] = new Dictionary<string, object?>
                {
                    ["type"] = "zeebe:taskHeader",
                    ["key"] = "assignee"
                }
            },
            new Dictionary<string, object?>
            {
                ["label"] = "Candidate Groups",
                ["description"] = "Comma-separated group codes or FEEL list expression for group queue.",
                ["type"] = "String",
                ["value"] = string.Empty,
                ["feel"] = "optional",
                ["binding"] = new Dictionary<string, object?>
                {
                    ["type"] = "zeebe:taskHeader",
                    ["key"] = "candidateGroups"
                }
            },
            new Dictionary<string, object?>
            {
                ["label"] = "Task Timeout (ISO-8601)",
                ["description"] = "ISO-8601 duration (e.g. P3D, PT4H). Triggers H2L_TASK_EXPIRED on timeout.",
                ["type"] = "String",
                ["value"] = "P3D",
                ["feel"] = "optional",
                ["binding"] = new Dictionary<string, object?>
                {
                    ["type"] = "zeebe:taskHeader",
                    ["key"] = "timeout"
                }
            },
            new Dictionary<string, object?>
            {
                ["label"] = "Sensitive Data Mode",
                ["description"] = "ClaimCheck (Privacy by Default) keeps isSensitive fields out of Zeebe history. PassThrough sends all values.",
                ["type"] = "Dropdown",
                ["value"] = "ClaimCheck",
                ["choices"] = new[]
                {
                    new Dictionary<string, string> { ["name"] = "ClaimCheck (Recommended / GDPR)", ["value"] = "ClaimCheck" },
                    new Dictionary<string, string> { ["name"] = "PassThrough (Plain-text in Zeebe)", ["value"] = "PassThrough" }
                },
                ["binding"] = new Dictionary<string, object?>
                {
                    ["type"] = "zeebe:taskHeader",
                    ["key"] = "sensitiveDataMode"
                }
            },
            new Dictionary<string, object?>
            {
                ["label"] = "Throw BPMN Error on Missing Assignee",
                ["description"] = "If true, throws H2L_ASSIGNEE_NOT_FOUND instead of routing task to NeedsAssignment.",
                ["type"] = "Dropdown",
                ["value"] = "false",
                ["choices"] = new[]
                {
                    new Dictionary<string, string> { ["name"] = "No (Route to NeedsAssignment)", ["value"] = "false" },
                    new Dictionary<string, string> { ["name"] = "Yes (Throw H2L_ASSIGNEE_NOT_FOUND)", ["value"] = "true" }
                },
                ["binding"] = new Dictionary<string, object?>
                {
                    ["type"] = "zeebe:taskHeader",
                    ["key"] = "throwOnMissingAssignee"
                }
            }
        };

        var template = new Dictionary<string, object?>
        {
            ["$schema"] = SchemaUrl,
            ["name"] = generic ? "h2l Human Task" : $"h2l: {formDefinition.Name}",
            ["id"] = generic ? "io.h2l.human-task.generic" : $"io.h2l.human-task.{formDefinition.Id:N}",
            ["version"] = versionNumber ?? 1,
            ["description"] = generic
                ? "Generic Human-in-the-Loop task handled by h2l."
                : $"Preconfigured h2l Human Task for '{formDefinition.Name}'. {variableHint}",
            ["appliesTo"] = new[] { "bpmn:ServiceTask" },
            ["elementType"] = new Dictionary<string, object?>
            {
                ["value"] = "bpmn:ServiceTask"
            },
            ["properties"] = properties
        };

        return JsonSerializer.SerializeToElement(template);
    }

    private static List<string> ExtractVariableKeys(FormSchemaDocument? schema)
    {
        var keys = new List<string>();
        if (schema is null)
        {
            return keys;
        }

        foreach (var step in schema.Steps)
        {
            CollectKeys(step.Components, keys);
        }

        return keys;
    }

    private static void CollectKeys(IReadOnlyList<FormComponentSchema>? components, List<string> keys)
    {
        if (components is null)
        {
            return;
        }

        foreach (var component in components)
        {
            if (!string.IsNullOrWhiteSpace(component.VariableKey))
            {
                keys.Add(component.VariableKey);
            }

            CollectKeys(component.Components, keys);
        }
    }
}
