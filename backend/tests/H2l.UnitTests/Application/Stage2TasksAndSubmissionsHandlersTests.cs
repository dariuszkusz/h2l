using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Forms.Expressions;
using H2l.Application.Forms.Schema;
using H2l.Application.Submissions.Queries;
using H2l.Application.Tasks;
using H2l.Application.Tasks.Commands;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Outbox;
using H2l.Domain.Tasks;
using H2l.Domain.Workflows;
using H2l.Infrastructure.Options;
using H2l.Infrastructure.Security;
using H2l.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class Stage2TasksAndSubmissionsHandlersTests
{
    private const string SchemaWithSensitiveAndFile = """
    {
      "schemaVersion": "1.0",
      "defaultLanguage": "en",
      "layoutMode": "single",
      "steps": [
        {
          "id": "step_1",
          "title": { "en": "Approval" },
          "components": [
            {
              "id": "cmp_name",
              "type": "text",
              "variableKey": "softwareName",
              "validators": [{ "type": "required" }]
            },
            {
              "id": "cmp_iban",
              "type": "text",
              "variableKey": "vendorIban",
              "isSensitive": true
            },
            {
              "id": "cmp_file",
              "type": "file",
              "variableKey": "quoteFile",
              "isSensitive": true,
              "fileConfig": {
                "allowedMimeTypes": ["application/pdf"],
                "maxSizeBytes": 1048576
              }
            }
          ]
        }
      ]
    }
    """;

    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IFormSchemaValidator _schemaValidator;
    private readonly IFormSubmissionValidator _submissionValidator;
    private readonly AesGcmEncryptionService _encryptionService;

    public Stage2TasksAndSubmissionsHandlersTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserContext.UserId.Returns(_userId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var evaluator = new SafeExpressionEvaluator();
        _schemaValidator = new FormSchemaValidator(evaluator);
        _submissionValidator = new FormSubmissionValidator(evaluator);

        var keyBytes = RandomNumberGenerator.GetBytes(32);
        var encOptions = Options.Create(new EncryptionOptions
        {
            KeyId = "v1",
            AesGcmKey = Convert.ToBase64String(keyBytes)
        });
        _encryptionService = new AesGcmEncryptionService(encOptions);
    }

    [Fact]
    public async Task ClaimTask_ConcurrentRace_ReturnsConflictTaskAlreadyClaimed()
    {
        var formDefId = Guid.CreateVersion7();
        var version = FormVersion.Create(_tenantId, formDefId, 1, SchemaWithSensitiveAndFile, _userId).Value;
        var task = HumanTask.Create(_tenantId, version.Id, "ext-1").Value;

        var taskRepo = Substitute.For<IHumanTaskRepository>();
        var versionRepo = Substitute.For<IFormVersionRepository>();
        var accessEvaluator = Substitute.For<ITaskAccessEvaluator>();
        var uow = Substitute.For<IUnitOfWork>();
        var notifier = Substitute.For<IRealtimeTaskNotifier>();

        taskRepo.GetByIdForUpdateAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        versionRepo.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);
        accessEvaluator.CanUserAccessTaskAsync(_userId, task, formDefId, Arg.Any<CancellationToken>()).Returns(true);
        uow.TrySaveChangesAsync(Arg.Any<CancellationToken>()).Returns(false);

        var sut = new ClaimTaskCommandHandler(
            _tenantContext, _currentUserContext, taskRepo, versionRepo, accessEvaluator, uow, notifier);

        var result = await sut.Handle(new ClaimTaskCommand(task.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("TASK_ALREADY_CLAIMED", result.Error.Code);
        await notifier.DidNotReceive().NotifyTaskUpdatedAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitTask_DuplicateIdempotencyKey_ReturnsExistingSubmissionIdempotently()
    {
        var taskId = Guid.CreateVersion7();
        var versionId = Guid.CreateVersion7();
        var existingSubmission = FormSubmission.Create(
            Guid.CreateVersion7(),
            _tenantId,
            taskId,
            versionId,
            _userId,
            "idem-key-123",
            "{\"softwareName\":\"Rider\"}",
            null,
            null,
            DateTimeOffset.UtcNow).Value;

        var submissionRepo = Substitute.For<IFormSubmissionRepository>();
        submissionRepo.GetByTaskAndIdempotencyKeyAsync(taskId, "idem-key-123", Arg.Any<CancellationToken>())
            .Returns(existingSubmission);

        var uow = Substitute.For<IUnitOfWork>();
        var sut = new SubmitTaskCommandHandler(
            _tenantContext,
            _currentUserContext,
            Substitute.For<IHumanTaskRepository>(),
            Substitute.For<IFormVersionRepository>(),
            submissionRepo,
            Substitute.For<ISubmissionAttachmentRepository>(),
            Substitute.For<IOutboxMessageRepository>(),
            Substitute.For<IInstanceSettingsRepository>(),
            Substitute.For<IAuditLogRepository>(),
            Substitute.For<ITaskAccessEvaluator>(),
            _schemaValidator,
            _submissionValidator,
            _encryptionService,
            uow,
            _dateTimeProvider,
            Substitute.For<IRealtimeTaskNotifier>());

        var result = await sut.Handle(
            new SubmitTaskCommand(taskId, "idem-key-123", new Dictionary<string, JsonElement>()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existingSubmission.Id, result.Value.SubmissionId);
        await uow.DidNotReceive().TrySaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitTask_SplitsSensitiveFieldsWithAesGcmAndCreatesOutboxAndAuditLog()
    {
        var formDefId = Guid.CreateVersion7();
        var version = FormVersion.Create(_tenantId, formDefId, 1, SchemaWithSensitiveAndFile, _userId).Value;
        var task = HumanTask.Create(_tenantId, version.Id, "ext-submit-1", assignedUserId: _userId).Value;

        var taskRepo = Substitute.For<IHumanTaskRepository>();
        var versionRepo = Substitute.For<IFormVersionRepository>();
        var submissionRepo = Substitute.For<IFormSubmissionRepository>();
        var attachmentRepo = Substitute.For<ISubmissionAttachmentRepository>();
        var outboxRepo = Substitute.For<IOutboxMessageRepository>();
        var settingsRepo = Substitute.For<IInstanceSettingsRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var accessEvaluator = Substitute.For<ITaskAccessEvaluator>();
        var uow = Substitute.For<IUnitOfWork>();
        var notifier = Substitute.For<IRealtimeTaskNotifier>();

        taskRepo.GetByIdForUpdateAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        versionRepo.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);
        accessEvaluator.CanUserAccessTaskAsync(_userId, task, formDefId, Arg.Any<CancellationToken>()).Returns(true);
        uow.TrySaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);

        FormSubmission? capturedSubmission = null;
        await submissionRepo.AddAsync(Arg.Do<FormSubmission>(s => capturedSubmission = s), Arg.Any<CancellationToken>());

        AuditLog? capturedAudit = null;
        await auditRepo.AddAsync(Arg.Do<AuditLog>(a => capturedAudit = a), Arg.Any<CancellationToken>());

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""
        {
          "softwareName": "JetBrains Rider",
          "vendorIban": "DE89370400440532013000"
        }
        """)!;

        var sut = new SubmitTaskCommandHandler(
            _tenantContext, _currentUserContext, taskRepo, versionRepo, submissionRepo,
            attachmentRepo, outboxRepo, settingsRepo, auditRepo, accessEvaluator,
            _schemaValidator, _submissionValidator, _encryptionService, uow, _dateTimeProvider, notifier);

        var result = await sut.Handle(
            new SubmitTaskCommand(task.Id, "idem-submit-1", values),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedSubmission);
        Assert.Contains("JetBrains Rider", capturedSubmission!.PublicDataJsonb);
        Assert.DoesNotContain("DE89370400440532013000", capturedSubmission.PublicDataJsonb);
        Assert.NotNull(capturedSubmission.EncryptedSensitiveData);
        Assert.DoesNotContain("DE89370400440532013000", capturedSubmission.EncryptedSensitiveData);

        var decrypted = _encryptionService.Decrypt(
            capturedSubmission.EncryptedSensitiveData!,
            $"submission:{_tenantId:N}:{capturedSubmission.Id:N}");
        Assert.Contains("DE89370400440532013000", decrypted);

        await outboxRepo.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m => m.HumanTaskId == task.Id && m.FormSubmissionId == capturedSubmission.Id),
            Arg.Any<CancellationToken>());
        Assert.NotNull(capturedAudit);
        Assert.DoesNotContain("DE89370400440532013000", capturedAudit!.MetadataJsonb ?? string.Empty);
    }

    [Fact]
    public async Task GetSubmissionSensitiveData_DecryptsAndAlwaysWritesAuditLogWithoutSensitiveValues()
    {
        var submissionId = Guid.CreateVersion7();
        var encrypted = _encryptionService.Encrypt(
            "{\"vendorIban\":\"DE89370400440532013000\"}",
            $"submission:{_tenantId:N}:{submissionId:N}");

        var submission = FormSubmission.Create(
            submissionId,
            _tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            _userId,
            "idem-1",
            "{}",
            encrypted,
            null,
            DateTimeOffset.UtcNow).Value;

        var submissionRepo = Substitute.For<IFormSubmissionRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        submissionRepo.GetByIdAsync(submissionId, Arg.Any<CancellationToken>()).Returns(submission);

        AuditLog? capturedAudit = null;
        await auditRepo.AddAsync(Arg.Do<AuditLog>(a => capturedAudit = a), Arg.Any<CancellationToken>());

        var sut = new GetSubmissionSensitiveDataQueryHandler(
            _tenantContext, _currentUserContext, submissionRepo, _encryptionService, auditRepo, uow, _dateTimeProvider);

        var result = await sut.Handle(new GetSubmissionSensitiveDataQuery(submissionId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("DE89370400440532013000", result.Value.Values.GetProperty("vendorIban").GetString());
        Assert.NotNull(capturedAudit);
        Assert.Equal("CLAIM_CHECK_SENSITIVE_DATA_READ", capturedAudit!.Action);
        Assert.DoesNotContain("DE89370400440532013000", capturedAudit.MetadataJsonb ?? string.Empty);
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadTaskAttachment_ValidatesMimeAndSizeAndEncryptsBytesOnDisk()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"h2l-att-test-{Guid.CreateVersion7():N}");
        try
        {
            var storageService = new EncryptedLocalAttachmentStorageService(
                Options.Create(new AttachmentsOptions
                {
                    MaxSizeMb = 10,
                    StoragePath = tempRoot
                }),
                _encryptionService);

            var formDefId = Guid.CreateVersion7();
            var version = FormVersion.Create(_tenantId, formDefId, 1, SchemaWithSensitiveAndFile, _userId).Value;
            var task = HumanTask.Create(_tenantId, version.Id, "ext-att-1", assignedUserId: _userId).Value;

            var taskRepo = Substitute.For<IHumanTaskRepository>();
            var versionRepo = Substitute.For<IFormVersionRepository>();
            var attachmentRepo = Substitute.For<ISubmissionAttachmentRepository>();
            var auditRepo = Substitute.For<IAuditLogRepository>();
            var accessEvaluator = Substitute.For<ITaskAccessEvaluator>();
            var uow = Substitute.For<IUnitOfWork>();

            taskRepo.GetByIdAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
            versionRepo.GetByIdAsync(version.Id, Arg.Any<CancellationToken>()).Returns(version);
            accessEvaluator.CanUserAccessTaskAsync(_userId, task, formDefId, Arg.Any<CancellationToken>()).Returns(true);

            var sut = new UploadTaskAttachmentCommandHandler(
                _tenantContext, _currentUserContext, taskRepo, versionRepo, attachmentRepo,
                auditRepo, accessEvaluator, _schemaValidator, _submissionValidator,
                storageService, uow, _dateTimeProvider);

            // Reject disallowed MIME type
            var badMime = await sut.Handle(
                new UploadTaskAttachmentCommand(task.Id, "quoteFile", "script.exe", "application/x-msdownload", 4, [1, 2, 3, 4]),
                CancellationToken.None);
            Assert.True(badMime.IsFailure);
            Assert.Equal("ATTACHMENT_MIME_TYPE_NOT_ALLOWED", badMime.Error.Code);

            // Reject oversized payload (> 1 MB component limit)
            var tooBig = await sut.Handle(
                new UploadTaskAttachmentCommand(task.Id, "quoteFile", "huge.pdf", "application/pdf", 2 * 1024 * 1024, [1, 2, 3, 4]),
                CancellationToken.None);
            Assert.True(tooBig.IsFailure);
            Assert.Equal(ErrorType.PayloadTooLarge, tooBig.Error.Type);

            // Valid upload with path-traversal filename sanitized & encrypted on disk
            var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.7 confidential quote");
            SubmissionAttachment? savedAttachment = null;
            await attachmentRepo.AddAsync(Arg.Do<SubmissionAttachment>(a => savedAttachment = a), Arg.Any<CancellationToken>());

            var ok = await sut.Handle(
                new UploadTaskAttachmentCommand(task.Id, "quoteFile", "../../evil/quote.pdf", "application/pdf", pdfBytes.Length, pdfBytes),
                CancellationToken.None);

            Assert.True(ok.IsSuccess);
            Assert.Equal("quote.pdf", ok.Value.OriginalFileName);
            Assert.NotNull(savedAttachment);
            Assert.True(savedAttachment!.IsEncrypted);
            Assert.DoesNotContain("..", savedAttachment.StoragePath);

            var rawDiskBytes = await File.ReadAllBytesAsync(Path.Combine(tempRoot, savedAttachment.StoragePath));
            Assert.DoesNotContain("confidential quote", Encoding.UTF8.GetString(rawDiskBytes));

            var decryptedBytes = await storageService.ReadDecryptedAsync(_tenantId, savedAttachment.Id, savedAttachment.StoragePath);
            Assert.Equal(pdfBytes, decryptedBytes);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
