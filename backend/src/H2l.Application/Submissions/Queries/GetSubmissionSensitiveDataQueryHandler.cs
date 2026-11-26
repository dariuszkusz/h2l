using System.Text.Json;
using H2l.Application.Common;
using H2l.Application.Submissions.DTOs;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Tasks;

namespace H2l.Application.Submissions.Queries;

public sealed class GetSubmissionSensitiveDataQueryHandler
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFormSubmissionRepository _formSubmissionRepository;
    private readonly IAesGcmEncryptionService _encryptionService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetSubmissionSensitiveDataQueryHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IFormSubmissionRepository formSubmissionRepository,
        IAesGcmEncryptionService encryptionService,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _formSubmissionRepository = formSubmissionRepository;
        _encryptionService = encryptionService;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SensitiveDataResponse>> Handle(
        GetSubmissionSensitiveDataQuery query,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.TenantId == Guid.Empty)
        {
            return Result.Failure<SensitiveDataResponse>(
                Error.Unauthorized("TENANT_CONTEXT_MISSING", "Tenant context is required."));
        }

        if (_currentUserContext.UserId is null || _currentUserContext.UserId == Guid.Empty)
        {
            return Result.Failure<SensitiveDataResponse>(
                Error.Unauthorized("USER_CONTEXT_MISSING", "Authenticated user context is required."));
        }

        var submission = await _formSubmissionRepository.GetByIdAsync(query.SubmissionId, cancellationToken);
        if (submission is null)
        {
            return Result.Failure<SensitiveDataResponse>(
                Error.NotFound("SUBMISSION_NOT_FOUND", "Form submission was not found."));
        }

        var tenantId = _tenantContext.TenantId.Value;
        JsonElement decryptedValues;
        var fieldCount = 0;

        if (string.IsNullOrWhiteSpace(submission.EncryptedSensitiveData))
        {
            using var emptyDoc = JsonDocument.Parse("{}");
            decryptedValues = emptyDoc.RootElement.Clone();
        }
        else
        {
            var decryptedJson = _encryptionService.Decrypt(
                submission.EncryptedSensitiveData,
                $"submission:{tenantId:N}:{submission.Id:N}");

            using var doc = JsonDocument.Parse(decryptedJson);
            decryptedValues = doc.RootElement.Clone();
            if (decryptedValues.ValueKind == JsonValueKind.Object)
            {
                fieldCount = decryptedValues.EnumerateObject().Count();
            }
        }

        var auditResult = AuditLog.Create(
            tenantId,
            _currentUserContext.UserId.Value,
            "CLAIM_CHECK_SENSITIVE_DATA_READ",
            "FormSubmission",
            submission.Id,
            JsonSerializer.Serialize(new
            {
                humanTaskId = submission.HumanTaskId,
                fieldCount
            }),
            _dateTimeProvider.UtcNow);

        if (auditResult.IsFailure)
        {
            return Result.Failure<SensitiveDataResponse>(auditResult.Error);
        }

        await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SensitiveDataResponse(submission.Id, decryptedValues));
    }
}
