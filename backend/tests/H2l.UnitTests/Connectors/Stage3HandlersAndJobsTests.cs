using H2l.Application.Common;
using H2l.Application.Connectors.Commands;
using H2l.Application.Connectors.Contracts;
using H2l.Application.Connectors.Services;
using H2l.Application.Licensing;
using H2l.Application.Tasks;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Connectors;
using H2l.Domain.Identity;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using H2l.Infrastructure.Jobs;
using H2l.Infrastructure.Options;
using H2l.Infrastructure.Security;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Connectors;

public sealed class Stage3HandlersAndJobsTests
{
    [Fact]
    public async Task CreateInboundHumanTask_UnauthorizedAssignee_RoutesToNeedsAssignmentDefenseInDepth()
    {
        var tenantId = Guid.CreateVersion7();
        var formId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;

        var encryption = new AesGcmEncryptionService(Options.Create(new EncryptionOptions
        {
            KeyId = "v1",
            AesGcmKey = Convert.ToBase64String(new byte[32])
        }));

        var connector = WorkflowConnector.Create(
            tenantId, "n8n", ConnectorType.N8n, ConnectorAuthType.HmacSha256,
            "https://n8n.example.com", null, null, false, false, true, now).Value;
        var formVersion = FormVersion.Create(tenantId, formId, 1, "{}", Guid.CreateVersion7()).Value;
        var unauthorizedUser = User.Create(tenantId, "outsider@example.com", "hash", SystemRole.EndUser).Value;

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);

        var connectorRepo = Substitute.For<IWorkflowConnectorRepository>();
        connectorRepo.GetByIdAsync(connector.Id, Arg.Any<CancellationToken>()).Returns(connector);

        var taskRepo = Substitute.For<IHumanTaskRepository>();
        var versionRepo = Substitute.For<IFormVersionRepository>();
        versionRepo.GetLatestByFormDefinitionIdAsync(formId, Arg.Any<CancellationToken>()).Returns(formVersion);

        var accessRuleRepo = Substitute.For<IFormAccessRuleRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByEmailOrExternalIdAsync("outsider@example.com", Arg.Any<CancellationToken>()).Returns(unauthorizedUser);

        var groupRepo = Substitute.For<IGroupRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var accessEvaluator = Substitute.For<ITaskAccessEvaluator>();
        accessEvaluator.CanUserAccessFormAsync(unauthorizedUser.Id, formId, Arg.Any<CancellationToken>()).Returns(false);

        var ssrfGuard = Substitute.For<ISsrfGuard>();
        ssrfGuard.ValidateUrlAsync("https://n8n.example.com/resume/abc", Arg.Any<CancellationToken>())
            .Returns(Result.Success(new Uri("https://n8n.example.com/resume/abc")));

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(now);
        var notifier = Substitute.For<IRealtimeTaskNotifier>();

        HumanTask? savedTask = null;
        await taskRepo.AddAsync(Arg.Do<HumanTask>(t => savedTask = t), Arg.Any<CancellationToken>());

        var handler = new CreateInboundHumanTaskCommandHandler(
            tenantContext,
            connectorRepo,
            taskRepo,
            versionRepo,
            accessRuleRepo,
            userRepo,
            groupRepo,
            auditRepo,
            accessEvaluator,
            ssrfGuard,
            encryption,
            unitOfWork,
            clock,
            notifier);

        var result = await handler.Handle(
            new CreateInboundHumanTaskCommand(
                connector.Id,
                "ext-1",
                formId,
                null,
                "outsider@example.com",
                null,
                null,
                "https://n8n.example.com/resume/abc",
                "P2D",
                SensitiveDataMode.ClaimCheck),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsDuplicate);
        Assert.Equal(HumanTaskStatus.NeedsAssignment, result.Value.Status);
        Assert.NotNull(savedTask);
        Assert.Null(savedTask!.AssignedUserId);
        Assert.NotNull(savedTask.EncryptedResumeUrl);
        Assert.DoesNotContain("n8n.example.com", savedTask.EncryptedResumeUrl);
    }

    [Fact]
    public async Task CreateConnector_FreePlanLimitExceeded_Returns402PaymentRequired()
    {
        var tenantId = Guid.CreateVersion7();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);

        var entitlement = Substitute.For<IEntitlementService>();
        entitlement.CanActivateConnectorAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.PaymentRequired("CONNECTOR_LIMIT_EXCEEDED", "Limit exceeded.")));

        var handler = new CreateConnectorCommandHandler(
            tenantContext,
            Substitute.For<ICurrentUserContext>(),
            Substitute.For<IWorkflowConnectorRepository>(),
            Substitute.For<IAuditLogRepository>(),
            entitlement,
            Substitute.For<ISsrfGuard>(),
            Substitute.For<IConnectorConfigSerializer>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IDateTimeProvider>());

        var result = await handler.Handle(
            new CreateConnectorCommand(
                "Second Connector",
                ConnectorType.N8n,
                "https://n8n.example.com",
                ConnectorAuthType.HmacSha256,
                null,
                null,
                null,
                IsActive: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.PaymentRequired, result.Error.Type);
        Assert.Equal("CONNECTOR_LIMIT_EXCEEDED", result.Error.Code);
    }

    [Fact]
    public async Task DataRetentionJob_AnonymizesExpiredSubmissionsAndDeletesEncryptedAttachments()
    {
        var tenantId = Guid.CreateVersion7();
        var taskId = Guid.CreateVersion7();
        var formVersionId = Guid.CreateVersion7();
        var submissionId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

        var submission = FormSubmission.Create(
            submissionId,
            tenantId,
            taskId,
            formVersionId,
            Guid.CreateVersion7(),
            "idem-ret-1",
            "{\"fullName\":\"Jan Kowalski\"}",
            "v1:enc:sensitive-data",
            now.AddDays(-1),
            now.AddDays(-91)).Value;

        var attachment = SubmissionAttachment.Create(
            Guid.CreateVersion7(),
            tenantId,
            taskId,
            "idScan",
            "scan.pdf",
            "application/pdf",
            1024,
            "tenant/file.enc",
            true,
            now.AddDays(-91)).Value;
        attachment.AttachToSubmission(submissionId);

        var submissionRepo = Substitute.For<IFormSubmissionRepository>();
        submissionRepo.ListExpiredForRetentionIgnoringTenantFilterAsync(now, 100, Arg.Any<CancellationToken>())
            .Returns([submission]);

        var attachmentRepo = Substitute.For<ISubmissionAttachmentRepository>();
        attachmentRepo.ListBySubmissionIdsIgnoringTenantFilterAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([attachment]);

        var storage = Substitute.For<IAttachmentStorageService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(now);

        var job = new DataRetentionJob(
            submissionRepo,
            attachmentRepo,
            storage,
            auditRepo,
            unitOfWork,
            clock);

        var count = await job.ProcessBatchAsync();

        Assert.Equal(1, count);
        Assert.Equal("{}", submission.PublicDataJsonb);
        Assert.Null(submission.EncryptedSensitiveData);
        Assert.Equal(now, submission.AnonymizedAt);
        await storage.Received(1).DeleteIfExistsAsync("tenant/file.enc", Arg.Any<CancellationToken>());
        attachmentRepo.Received(1).RemoveRange(Arg.Any<IEnumerable<SubmissionAttachment>>());
        await auditRepo.Received(1).AddAsync(
            Arg.Is<AuditLog>(a => a.Action == "SUBMISSION_RETENTION_ANONYMIZED"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TaskExpirationJob_ExpiresOverdueTasksAndEnqueuesOutboxMessage()
    {
        var tenantId = Guid.CreateVersion7();
        var connectorId = Guid.CreateVersion7();
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

        var overdueTask = HumanTask.Create(
            tenantId,
            Guid.CreateVersion7(),
            "job-exp-1",
            connectorId,
            expiresAt: now.AddMinutes(-5),
            createdAt: now.AddDays(-1)).Value;

        var taskRepo = Substitute.For<IHumanTaskRepository>();
        taskRepo.ListExpiredOpenTasksIgnoringTenantFilterAsync(now, 100, Arg.Any<CancellationToken>())
            .Returns([overdueTask]);

        var outboxRepo = Substitute.For<IOutboxMessageRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(now);
        var notifier = Substitute.For<IRealtimeTaskNotifier>();

        var job = new TaskExpirationJob(
            taskRepo,
            outboxRepo,
            auditRepo,
            unitOfWork,
            clock,
            notifier);

        var count = await job.ProcessBatchAsync();

        Assert.Equal(1, count);
        Assert.Equal(HumanTaskStatus.Expired, overdueTask.Status);
        await outboxRepo.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m => m.MessageType == OutboxDeliveryJob.TaskExpiredMessageType && m.HumanTaskId == overdueTask.Id),
            Arg.Any<CancellationToken>());
        await notifier.Received(1).NotifyTaskUpdatedAsync(tenantId, overdueTask.Id, "TaskExpired", Arg.Any<CancellationToken>());
    }
}
