using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.Services;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Connectors;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using H2l.Infrastructure.Jobs;
using H2l.Infrastructure.Options;
using H2l.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Connectors;

public sealed class ConnectorPayloadAndDeliveryTests
{
    private readonly ConnectorPayloadBuilder _payloadBuilder = new();

    [Fact]
    public void PayloadBuilder_ClaimCheckMode_ExcludesSensitiveValuesAndIncludesH2lMetadata()
    {
        var taskId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var context = new ConnectorTaskContext(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            taskId,
            "job-100",
            2,
            SensitiveDataMode.ClaimCheck,
            "https://n8n.example.com/resume/1",
            $"h2l:{taskId:N}:completed");

        var payload = new ConnectorTaskCompletionPayload(
            submissionId,
            new Dictionary<string, JsonElement>
            {
                ["softwareName"] = JsonSerializer.SerializeToElement("Rider")
            },
            new Dictionary<string, JsonElement>
            {
                ["vendorIban"] = JsonSerializer.SerializeToElement("PL1234567890")
            },
            ["vendorIban"],
            SensitiveDataMode.ClaimCheck);

        var webhookJson = _payloadBuilder.BuildWebhookCompletionBody(context, payload);
        using var doc = JsonDocument.Parse(webhookJson);

        Assert.Equal("completed", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Rider", doc.RootElement.GetProperty("data").GetProperty("softwareName").GetString());
        Assert.False(doc.RootElement.GetProperty("data").TryGetProperty("vendorIban", out _));
        Assert.Equal(submissionId, doc.RootElement.GetProperty("_h2l").GetProperty("submissionId").GetGuid());
        Assert.Equal("vendorIban", doc.RootElement.GetProperty("_h2l").GetProperty("sensitiveKeys")[0].GetString());

        var camundaVars = _payloadBuilder.BuildCamundaCompletionVariables(context, payload);
        Assert.True(camundaVars.ContainsKey("softwareName"));
        Assert.False(camundaVars.ContainsKey("vendorIban"));
        Assert.True(camundaVars.ContainsKey("_h2l"));
    }

    [Fact]
    public void PayloadBuilder_PassThroughMode_IncludesSensitiveValues()
    {
        var taskId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var context = new ConnectorTaskContext(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            taskId,
            "job-101",
            1,
            SensitiveDataMode.PassThrough,
            null,
            $"h2l:{taskId:N}:completed");

        var payload = new ConnectorTaskCompletionPayload(
            submissionId,
            new Dictionary<string, JsonElement>
            {
                ["softwareName"] = JsonSerializer.SerializeToElement("Rider")
            },
            new Dictionary<string, JsonElement>
            {
                ["vendorIban"] = JsonSerializer.SerializeToElement("PL1234567890")
            },
            ["vendorIban"],
            SensitiveDataMode.PassThrough);

        var webhookJson = _payloadBuilder.BuildWebhookCompletionBody(context, payload);
        using var doc = JsonDocument.Parse(webhookJson);
        Assert.Equal("PL1234567890", doc.RootElement.GetProperty("data").GetProperty("vendorIban").GetString());

        var camundaVars = _payloadBuilder.BuildCamundaCompletionVariables(context, payload);
        Assert.True(camundaVars.ContainsKey("vendorIban"));
        Assert.False(camundaVars.ContainsKey("_h2l"));
    }

    [Theory]
    [InlineData(408, true)]
    [InlineData(429, true)]
    [InlineData(500, true)]
    [InlineData(503, true)]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(403, false)]
    [InlineData(404, false)]
    [InlineData(422, false)]
    public void ClassifyHttpFailure_DistinguishesTransientRetryVsPermanentDeadLetter(int statusCode, bool expectedTransient)
    {
        var result = ConnectorDeliveryResult.ClassifyHttpFailure(statusCode, "Status");

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedTransient, result.IsTransientError);
        Assert.Equal(statusCode, result.HttpStatusCode);
    }

    [Fact]
    public async Task OutboxDeliveryJob_Transient503RetriesWithBackoff_Hard400MovesToDeadLetter_SuccessClearsResumeUrl()
    {
        var tenantId = Guid.CreateVersion7();
        var formVersionId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

        var encryption = new AesGcmEncryptionService(Options.Create(new EncryptionOptions
        {
            KeyId = "v1",
            AesGcmKey = Convert.ToBase64String(new byte[32])
        }));
        var configSerializer = new ConnectorConfigSerializer(encryption);

        var connector = WorkflowConnector.Create(
            tenantId,
            "n8n-prod",
            ConnectorType.N8n,
            ConnectorAuthType.HmacSha256,
            "https://n8n.example.com",
            null,
            null,
            false,
            false,
            true,
            now).Value;

        var encryptedResume = encryption.Encrypt(
            "https://n8n.example.com/webhook-waiting/exec-42",
            $"task-resume:{tenantId:N}:{connector.Id:N}");

        var task = HumanTask.Create(
            tenantId,
            formVersionId,
            "exec-42",
            connector.Id,
            SensitiveDataMode.ClaimCheck,
            encryptedResumeUrl: encryptedResume,
            createdAt: now).Value;

        var submissionId = Guid.CreateVersion7();
        var submission = FormSubmission.Create(
            submissionId,
            tenantId,
            task.Id,
            formVersionId,
            Guid.CreateVersion7(),
            "idem-1",
            "{\"approved\":true}",
            null,
            null,
            now).Value;

        var message = OutboxMessage.Create(tenantId, task.Id, submissionId, OutboxDeliveryJob.TaskSubmittedMessageType, now).Value;

        var outboxRepo = Substitute.For<IOutboxMessageRepository>();
        var taskRepo = Substitute.For<IHumanTaskRepository>();
        var submissionRepo = Substitute.For<IFormSubmissionRepository>();
        var versionRepo = Substitute.For<IFormVersionRepository>();
        var connectorRepo = Substitute.For<IWorkflowConnectorRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var registry = Substitute.For<IWorkflowConnectorRegistry>();
        var connectorImpl = Substitute.For<IWorkflowConnector>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();

        clock.UtcNow.Returns(now);
        outboxRepo.ListPendingBatchIgnoringTenantFilterAsync(now, 50, Arg.Any<CancellationToken>())
            .Returns([message]);
        taskRepo.GetByIdIgnoringTenantFilterForUpdateAsync(task.Id, Arg.Any<CancellationToken>())
            .Returns(task);
        submissionRepo.GetByIdIgnoringTenantFilterAsync(submissionId, Arg.Any<CancellationToken>())
            .Returns(submission);
        connectorRepo.GetByIdIgnoringTenantFilterAsync(connector.Id, Arg.Any<CancellationToken>())
            .Returns(connector);
        registry.GetConnector(ConnectorType.N8n).Returns(connectorImpl);

        var job = new OutboxDeliveryJob(
            outboxRepo,
            taskRepo,
            submissionRepo,
            versionRepo,
            connectorRepo,
            auditRepo,
            registry,
            configSerializer,
            encryption,
            unitOfWork,
            clock,
            Options.Create(new ConnectorSecurityOptions { MaxOutboxRetries = 3 }),
            NullLogger<OutboxDeliveryJob>.Instance);

        // 1. Transient 503 -> retry with backoff, resumeUrl preserved
        connectorImpl.CompleteTaskAsync(
                Arg.Any<ConnectorRuntimeConfig>(),
                Arg.Any<ConnectorTaskContext>(),
                Arg.Any<ConnectorTaskCompletionPayload>(),
                Arg.Any<CancellationToken>())
            .Returns(ConnectorDeliveryResult.ClassifyHttpFailure(503, "Service Unavailable"));

        await job.ProcessBatchAsync();

        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(1, message.RetryCount);
        Assert.True(message.NextAttemptAt > now);
        Assert.NotNull(task.EncryptedResumeUrl);

        // 2. Hard 400 -> immediate DeadLetter
        connectorImpl.CompleteTaskAsync(
                Arg.Any<ConnectorRuntimeConfig>(),
                Arg.Any<ConnectorTaskContext>(),
                Arg.Any<ConnectorTaskCompletionPayload>(),
                Arg.Any<CancellationToken>())
            .Returns(ConnectorDeliveryResult.ClassifyHttpFailure(400, "Bad Request"));

        await job.ProcessBatchAsync();

        Assert.Equal(OutboxMessageStatus.DeadLetter, message.Status);
        Assert.Contains("400", message.DeadLetterReason);

        // 3. Manual retry + success -> Processed AND EncryptedResumeUrl permanently deleted!
        Assert.True(message.RetryFromDeadLetter(now).IsSuccess);
        connectorImpl.CompleteTaskAsync(
                Arg.Any<ConnectorRuntimeConfig>(),
                Arg.Any<ConnectorTaskContext>(),
                Arg.Any<ConnectorTaskCompletionPayload>(),
                Arg.Any<CancellationToken>())
            .Returns(ConnectorDeliveryResult.Success(200));

        await job.ProcessBatchAsync();

        Assert.Equal(OutboxMessageStatus.Processed, message.Status);
        Assert.Null(task.EncryptedResumeUrl);
    }
}
