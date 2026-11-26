using System.Text.Json;
using H2l.Application.Connectors.Services;
using H2l.Application.Forms.Schema;
using H2l.Domain.Workflows;
using Xunit;

namespace H2l.UnitTests.Connectors;

public sealed class TemplateGeneratorsTests
{
    [Fact]
    public void CamundaElementTemplateGenerator_ProducesGenericAndPreconfiguredTemplatesWithFeel()
    {
        var tenantId = Guid.CreateVersion7();
        var processId = Guid.CreateVersion7();
        var form = FormDefinition.Create(tenantId, processId, "Software Purchase", "software_purchase").Value;
        var version = FormVersion.Create(tenantId, form.Id, 3, "{}", Guid.CreateVersion7()).Value;
        var schema = new FormSchemaDocument(
            "1.0",
            "en",
            "single",
            [
                new FormStepSchema("s1", new LocalizedTextSchema("Step 1"), null,
                [
                    new FormComponentSchema("c1", "text", VariableKey: "softwareName"),
                    new FormComponentSchema("c2", "number", VariableKey: "estimatedCost")
                ])
            ]);

        var generator = new CamundaElementTemplateGenerator();

        var preconfigured = generator.Generate(form, version, schema, generic: false);
        Assert.Equal($"io.h2l.human-task.{form.Id:N}", preconfigured.GetProperty("id").GetString());
        Assert.Equal(3, preconfigured.GetProperty("version").GetInt32());
        Assert.Contains("softwareName", preconfigured.GetProperty("description").GetString());

        var props = preconfigured.GetProperty("properties").EnumerateArray().ToList();
        Assert.Contains(props, p => p.GetProperty("value").GetString() == "h2l-human-task");
        var formIdProp = props.Single(p =>
            p.GetProperty("binding").TryGetProperty("key", out var k) && k.GetString() == "formId");
        Assert.Equal(form.Id.ToString(), formIdProp.GetProperty("value").GetString());
        Assert.Equal("optional", formIdProp.GetProperty("feel").GetString());

        var generic = generator.Generate(form, version, schema, generic: true);
        Assert.Equal("io.h2l.human-task.generic", generic.GetProperty("id").GetString());
        var genericFormIdProp = generic.GetProperty("properties").EnumerateArray().Single(p =>
            p.GetProperty("binding").TryGetProperty("key", out var k) && k.GetString() == "formId");
        Assert.Equal(string.Empty, genericFormIdProp.GetProperty("value").GetString());
    }

    [Fact]
    public void N8nWorkflowTemplateGenerator_ProducesCommunityNodeAndFallbackWaitNodeVariants()
    {
        var tenantId = Guid.CreateVersion7();
        var processId = Guid.CreateVersion7();
        var connectorId = Guid.CreateVersion7();
        var form = FormDefinition.Create(tenantId, processId, "Expense Approval", "expense_approval").Value;
        var version = FormVersion.Create(tenantId, form.Id, 2, "{}", Guid.CreateVersion7()).Value;
        var schema = new FormSchemaDocument(
            "1.0",
            "en",
            "single",
            [
                new FormStepSchema("s1", new LocalizedTextSchema("Step 1"), null,
                [
                    new FormComponentSchema("c1", "text", VariableKey: "employeeName")
                ])
            ]);

        var generator = new N8nWorkflowTemplateGenerator();

        var community = generator.Generate(form, version, schema, N8nWorkflowTemplateGenerator.CommunityNodeVariant, connectorId);
        var communityNodes = community.GetProperty("nodes").EnumerateArray().ToList();
        Assert.Single(communityNodes);
        Assert.Equal("n8n-nodes-h2l.h2lHumanTask", communityNodes[0].GetProperty("type").GetString());
        Assert.Equal(form.Id.ToString(), communityNodes[0].GetProperty("parameters").GetProperty("formId").GetString());
        Assert.Equal(connectorId.ToString(), communityNodes[0].GetProperty("parameters").GetProperty("connectorId").GetString());

        var fallback = generator.Generate(form, version, schema, N8nWorkflowTemplateGenerator.FallbackWaitNodeVariant, connectorId);
        var fallbackNodes = fallback.GetProperty("nodes").EnumerateArray().ToList();
        Assert.Equal(3, fallbackNodes.Count);
        Assert.Equal("n8n-nodes-base.httpRequest", fallbackNodes[0].GetProperty("type").GetString());
        Assert.Equal("n8n-nodes-base.wait", fallbackNodes[1].GetProperty("type").GetString());
        Assert.Equal("n8n-nodes-base.switch", fallbackNodes[2].GetProperty("type").GetString());
        Assert.Contains("$execution.resumeUrl", fallbackNodes[0].GetProperty("parameters").GetProperty("jsonBody").GetString());
    }
}
