using H2l.Application.Common;
using H2l.Application.Forms.Commands;
using H2l.Application.Licensing;
using H2l.Application.Projects.Commands;
using H2l.Application.Projects.Mappings;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using H2l.Domain.Workflows;
using Mapster;
using MapsterMapper;
using NSubstitute;
using Xunit;

namespace H2l.UnitTests.Application;

public sealed class ProjectsAndFormsHandlersTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IMapper _mapper;

    public ProjectsAndFormsHandlersTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUserContext.UserId.Returns(Guid.CreateVersion7());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var config = new TypeAdapterConfig();
        new ProjectsMappingConfig().Register(config);
        _mapper = new Mapper(config);
    }

    [Fact]
    public async Task CreateWorkflowProcess_ActiveWhenFreeLimitExceeded_ReturnsPaymentRequired()
    {
        var project = Project.Create(_tenantId, "HR").Value;
        var projectRepo = Substitute.For<IProjectRepository>();
        var processRepo = Substitute.For<IWorkflowProcessRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var entitlement = Substitute.For<IEntitlementService>();
        var uow = Substitute.For<IUnitOfWork>();

        projectRepo.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        entitlement.CanActivateWorkflowProcessAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.PaymentRequired("WORKFLOW_PROCESS_LIMIT_EXCEEDED", "Limit reached.")));

        var sut = new CreateWorkflowProcessCommandHandler(
            _tenantContext, _currentUserContext, projectRepo, processRepo, auditRepo,
            entitlement, uow, _dateTimeProvider, _mapper);

        var result = await sut.Handle(
            new CreateWorkflowProcessCommand(project.Id, "Purchase Approval", "purchase-approval", null, WorkflowProcessStatus.Active),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.PaymentRequired, result.Error.Type);
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateFormAccessRules_SupportsWholeGroupSubsetAndDirectUserAndRejectsNonMemberSubset()
    {
        var processId = Guid.CreateVersion7();
        var form = FormDefinition.Create(_tenantId, processId, "Approval Step", "step-approval").Value;
        var groupHr = Group.Create(_tenantId, "HR", "hr").Value;
        var groupSales = Group.Create(_tenantId, "Sales", "sales").Value;
        var userInSales = User.Create(_tenantId, "sales1@example.com", "hash", SystemRole.EndUser).Value;
        var directUser = User.Create(_tenantId, "director@example.com", "hash", SystemRole.EndUser).Value;

        var formRepo = Substitute.For<IFormDefinitionRepository>();
        var ruleRepo = Substitute.For<IFormAccessRuleRepository>();
        var groupRepo = Substitute.For<IGroupRepository>();
        var userRepo = Substitute.For<IUserRepository>();
        var auditRepo = Substitute.For<IAuditLogRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        formRepo.GetByIdAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        groupRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyCollection<Guid>>();
                return new[] { groupHr, groupSales }.Where(g => ids.Contains(g.Id)).ToList();
            });
        userRepo.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyCollection<Guid>>();
                return new[] { userInSales, directUser }.Where(u => ids.Contains(u.Id)).ToList();
            });
        groupRepo.GetExistingMembershipsAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<CancellationToken>())
            .Returns(new HashSet<(Guid, Guid)> { (groupSales.Id, userInSales.Id) });
        ruleRepo.ListByFormDefinitionIdForUpdateAsync(form.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<FormAccessRule>());

        var sut = new UpdateFormAccessRulesCommandHandler(
            _tenantContext, _currentUserContext, formRepo, ruleRepo, groupRepo, userRepo,
            auditRepo, uow, _dateTimeProvider);

        // Valid combination of WholeGroup + GroupUserSubset + DirectUser
        var validResult = await sut.Handle(
            new UpdateFormAccessRulesCommand(
                form.Id,
                [
                    new UpdateFormAccessRuleItemRequest(groupHr.Id, null),
                    new UpdateFormAccessRuleItemRequest(groupSales.Id, userInSales.Id),
                    new UpdateFormAccessRuleItemRequest(null, directUser.Id)
                ]),
            CancellationToken.None);

        Assert.True(validResult.IsSuccess);
        await ruleRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<FormAccessRule>>(rules => rules.Count() == 3),
            Arg.Any<CancellationToken>());

        // Reject subset where user is NOT a member of the specified group
        var invalidResult = await sut.Handle(
            new UpdateFormAccessRulesCommand(
                form.Id,
                [new UpdateFormAccessRuleItemRequest(groupHr.Id, directUser.Id)]),
            CancellationToken.None);

        Assert.True(invalidResult.IsFailure);
        Assert.Equal("USER_NOT_IN_GROUP", invalidResult.Error.Code);
    }
}
