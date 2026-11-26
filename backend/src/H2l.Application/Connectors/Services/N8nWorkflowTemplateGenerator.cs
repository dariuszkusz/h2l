using System.Text.Json;
using H2l.Application.Forms.Schema;
using H2l.Domain.Workflows;

namespace H2l.Application.Connectors.Services;

public sealed class N8nWorkflowTemplateGenerator : IN8nWorkflowTemplateGenerator
{
    public const string CommunityNodeVariant = "community-node";
    public const string FallbackWaitNodeVariant = "fallback-wait-node";

    public JsonElement Generate(
        FormDefinition formDefinition,
        FormVersion? latestPublishedVersion,
        FormSchemaDocument? parsedSchema,
        string variant,
        Guid? connectorId)
    {
        var connectorPlaceholder = connectorId.HasValue
            ? connectorId.Value.ToString()
            : "{{ $env.H2L_CONNECTOR_ID }}";
        var formVersion = latestPublishedVersion?.VersionNumber;
        var variableKeys = ExtractVariableKeys(parsedSchema);

        if (string.Equals(variant, FallbackWaitNodeVariant, StringComparison.OrdinalIgnoreCase))
        {
            return BuildFallbackWorkflow(formDefinition, formVersion, connectorPlaceholder, variableKeys);
        }

        return BuildCommunityNodeWorkflow(formDefinition, formVersion, connectorPlaceholder, variableKeys);
    }

    private static JsonElement BuildCommunityNodeWorkflow(
        FormDefinition formDefinition,
        int? formVersion,
        string connectorPlaceholder,
        List<string> variableKeys)
    {
        var workflow = new Dictionary<string, object?>
        {
            ["name"] = $"h2l — {formDefinition.Name}",
            ["nodes"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = Guid.CreateVersion7().ToString(),
                    ["name"] = $"h2l: {formDefinition.Name}",
                    ["type"] = "n8n-nodes-h2l.h2lHumanTask",
                    ["typeVersion"] = 1,
                    ["position"] = new[] { 320, 240 },
                    ["parameters"] = new Dictionary<string, object?>
                    {
                        ["operation"] = "waitForHumanForm",
                        ["connectorId"] = connectorPlaceholder,
                        ["formId"] = formDefinition.Id.ToString(),
                        ["formVersion"] = formVersion,
                        ["assignee"] = string.Empty,
                        ["candidateGroups"] = string.Empty,
                        ["timeoutIso8601"] = "P3D",
                        ["sensitiveDataMode"] = "ClaimCheck",
                        ["prefilledData"] = BuildPrefilledPlaceholder(variableKeys)
                    },
                    ["credentials"] = new Dictionary<string, object?>
                    {
                        ["h2lApi"] = new Dictionary<string, object?>
                        {
                            ["name"] = "h2l Credentials"
                        }
                    }
                }
            },
            ["connections"] = new Dictionary<string, object?>(),
            ["meta"] = new Dictionary<string, object?>
            {
                ["h2lVariableKeys"] = variableKeys,
                ["timeoutNote"] = "Ensure n8n execution timeout is disabled or longer than h2l task timeout."
            }
        };

        return JsonSerializer.SerializeToElement(workflow);
    }

    private static JsonElement BuildFallbackWorkflow(
        FormDefinition formDefinition,
        int? formVersion,
        string connectorPlaceholder,
        List<string> variableKeys)
    {
        var requestNodeName = $"Create h2l Task ({formDefinition.Name})";
        var waitNodeName = "Wait for h2l Callback";
        var switchNodeName = "Route h2l Status";

        var bodyPayload = new Dictionary<string, object?>
        {
            ["externalTaskId"] = "={{ $execution.id }}",
            ["formId"] = formDefinition.Id.ToString(),
            ["formVersion"] = formVersion,
            ["resumeUrl"] = "={{ $execution.resumeUrl }}",
            ["timeoutIso8601"] = "P3D",
            ["sensitiveDataMode"] = "ClaimCheck",
            ["prefilledData"] = BuildPrefilledPlaceholder(variableKeys)
        };

        var workflow = new Dictionary<string, object?>
        {
            ["name"] = $"h2l Fallback — {formDefinition.Name}",
            ["nodes"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = Guid.CreateVersion7().ToString(),
                    ["name"] = requestNodeName,
                    ["type"] = "n8n-nodes-base.httpRequest",
                    ["typeVersion"] = 4.2,
                    ["position"] = new[] { 240, 240 },
                    ["parameters"] = new Dictionary<string, object?>
                    {
                        ["method"] = "POST",
                        ["url"] = $"={{{{ $env.H2L_BASE_URL }}}}/api/v1/webhooks/connectors/{connectorPlaceholder}/tasks",
                        ["sendHeaders"] = true,
                        ["headerParameters"] = new Dictionary<string, object?>
                        {
                            ["parameters"] = new[]
                            {
                                new Dictionary<string, object?>
                                {
                                    ["name"] = "X-API-Key",
                                    ["value"] = "={{ $env.H2L_API_KEY }}"
                                }
                            }
                        },
                        ["sendBody"] = true,
                        ["specifyBody"] = "json",
                        ["jsonBody"] = JsonSerializer.Serialize(bodyPayload)
                    }
                },
                new Dictionary<string, object?>
                {
                    ["id"] = Guid.CreateVersion7().ToString(),
                    ["name"] = waitNodeName,
                    ["type"] = "n8n-nodes-base.wait",
                    ["typeVersion"] = 1.1,
                    ["position"] = new[] { 500, 240 },
                    ["parameters"] = new Dictionary<string, object?>
                    {
                        ["resume"] = "webhook",
                        ["httpMethod"] = "POST",
                        ["responseMode"] = "onReceived"
                    }
                },
                new Dictionary<string, object?>
                {
                    ["id"] = Guid.CreateVersion7().ToString(),
                    ["name"] = switchNodeName,
                    ["type"] = "n8n-nodes-base.switch",
                    ["typeVersion"] = 3.2,
                    ["position"] = new[] { 760, 240 },
                    ["parameters"] = new Dictionary<string, object?>
                    {
                        ["rules"] = new Dictionary<string, object?>
                        {
                            ["values"] = new[]
                            {
                                CreateSwitchRule("completed"),
                                CreateSwitchRule("expired"),
                                CreateSwitchRule("cancelled")
                            }
                        }
                    }
                }
            },
            ["connections"] = new Dictionary<string, object?>
            {
                [requestNodeName] = new Dictionary<string, object?>
                {
                    ["main"] = new[]
                    {
                        new[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["node"] = waitNodeName,
                                ["type"] = "main",
                                ["index"] = 0
                            }
                        }
                    }
                },
                [waitNodeName] = new Dictionary<string, object?>
                {
                    ["main"] = new[]
                    {
                        new[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["node"] = switchNodeName,
                                ["type"] = "main",
                                ["index"] = 0
                            }
                        }
                    }
                }
            },
            ["meta"] = new Dictionary<string, object?>
            {
                ["h2lVariableKeys"] = variableKeys,
                ["timeoutNote"] = "Configure Wait Node timeout to be disabled or greater than h2l task timeout."
            }
        };

        return JsonSerializer.SerializeToElement(workflow);
    }

    private static Dictionary<string, object?> CreateSwitchRule(string status)
        => new()
        {
            ["outputKey"] = status,
            ["conditions"] = new Dictionary<string, object?>
            {
                ["conditions"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["leftValue"] = "={{ $json.body.status ?? $json.status }}",
                        ["rightValue"] = status,
                        ["operator"] = new Dictionary<string, object?>
                        {
                            ["type"] = "string",
                            ["operation"] = "equals"
                        }
                    }
                }
            }
        };

    private static Dictionary<string, object?> BuildPrefilledPlaceholder(List<string> variableKeys)
    {
        var map = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var key in variableKeys)
        {
            map[key] = null;
        }

        return map;
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
