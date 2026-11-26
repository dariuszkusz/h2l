using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Forms.Commands;
using H2l.Application.Forms.Expressions;
using H2l.Application.Forms.Schema;
using H2l.Application.Licensing;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Workflows;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class Stage2FormsHandlersTests
{
    private const string ValidSchemaJson = """
    {
      "schemaVersion": "1.0",
      "defaultLanguage": "en",
      "layoutMode": "single",
      "steps": [
        {
          "id": "step_1",
          "title": { "en": "Step 1" },
          "components": [
            { "id": "cmp_1", "type": "text", "variableKey": "requestTitle" }
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
    private readonly IFormSchemaValidator _schemaValidator = new FormSchemaValidator(new SafeExpressionEvaluator());

    public Stage2FormsHandlersTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserContext.UserId.Returns(_userId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task PublishFormVersion_WhenFreePlanProcessLimitExceeded_ReturnsPaymentRequired()
    {
        var projectId = Guid.CreateVersion7();
        var process = WorkflowProcess.Create(_tenantId, projectId, "Proc 2", "proc-2", status: WorkflowProcessStatus.Draft).Value;
        var form = FormDefinition.Create(_tenantId, process.Id, "Form 1", "step-1", ValidSchemaJson).Value;

        var formRepo = Substitute.For<IFormDefinitionRepository>();
        var processRepo = Substitute.For<IWorkflowProcessRepository>();
        var versionRepo = Substitute.For<IFormVersionRepository>();
        var entitlement = Substitute.For<IEntitlementService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        formRepo.GetByIdForUpdateAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        processRepo.GetByIdForUpdateAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);
        entitlement.CanActivateWorkflowProcessAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.PaymentRequired("WORKFLOW_PROCESS_LIMIT_EXCEEDED", "Upgrade plan.")));

        var sut = new PublishFormVersionCommandHandler(
            _tenantContext, _currentUserContext, formRepo, processRepo, versionRepo,
            _schemaValidator, entitlement, auditRepo, uow, _dateTimeProvider);

        var result = await sut.Handle(new PublishFormVersionCommand(form.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.PaymentRequired, result.Error.Type);
        await versionRepo.DidNotReceive().AddAsync(Arg.Any<FormVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishFormVersion_CreatesImmutableVersionAndIncrementsVersionNumber()
    {
        var projectId = Guid.CreateVersion7();
        var process = WorkflowProcess.Create(_tenantId, projectId, "Proc 1", "proc-1", status: WorkflowProcessStatus.Active).Value;
        var form = FormDefinition.Create(_tenantId, process.Id, "Form 1", "step-1", ValidSchemaJson).Value;

        var formRepo = Substitute.For<IFormDefinitionRepository>();
        var processRepo = Substitute.For<IWorkflowProcessRepository>();
        var versionRepo = Substitute.For<IFormVersionRepository>();
        var entitlement = Substitute.For<IEntitlementService>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        formRepo.GetByIdForUpdateAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        processRepo.GetByIdForUpdateAsync(process.Id, Arg.Any<CancellationToken>()).Returns(process);
        versionRepo.GetLatestVersionNumberAsync(form.Id, Arg.Any<CancellationToken>()).Returns(1);

        var sut = new PublishFormVersionCommandHandler(
            _tenantContext, _currentUserContext, formRepo, processRepo, versionRepo,
            _schemaValidator, entitlement, auditRepo, uow, _dateTimeProvider);

        var result = await sut.Handle(new PublishFormVersionCommand(form.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.VersionNumber);
        Assert.Equal(FormDefinitionStatus.Published, form.Status);
        await versionRepo.Received(1).AddAsync(Arg.Is<FormVersion>(v => v.VersionNumber == 2), Arg.Any<CancellationToken>());
        await auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a => a.Action == "FORM_VERSION_PUBLISHED"), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateFormDraft_ValidatesSchemaAndWritesAuditLog()
    {
        var form = FormDefinition.Create(_tenantId, Guid.CreateVersion7(), "Old Name", "step-1").Value;
        var formRepo = Substitute.For<IFormDefinitionRepository>();
        var versionRepo = Substitute.For<IFormVersionRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        formRepo.GetByIdForUpdateAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        versionRepo.GetLatestVersionNumberAsync(form.Id, Arg.Any<CancellationToken>()).Returns((int?)null);

        using var doc = JsonDocument.Parse(ValidSchemaJson);
        var sut = new UpdateFormDraftCommandHandler(
            _tenantContext, _currentUserContext, formRepo, versionRepo,
            _schemaValidator, auditRepo, uow, _dateTimeProvider);

        var result = await sut.Handle(
            new UpdateFormDraftCommand(form.Id, "Updated Name", doc.RootElement.Clone()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", result.Value.Name);
        await auditRepo.Received(1).AddAsync(Arg.Is<AuditLog>(a => a.Action == "FORM_DRAFT_UPDATED"), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
