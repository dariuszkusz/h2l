using System.Text.Json;
using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Tasks;
using Quartz;

namespace H2l.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class DataRetentionJob : IJob
{
    private readonly IFormSubmissionRepository _formSubmissionRepository;
    private readonly ISubmissionAttachmentRepository _attachmentRepository;
    private readonly IAttachmentStorageService _attachmentStorageService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DataRetentionJob(
        IFormSubmissionRepository formSubmissionRepository,
        ISubmissionAttachmentRepository attachmentRepository,
        IAttachmentStorageService attachmentStorageService,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _formSubmissionRepository = formSubmissionRepository;
        _attachmentRepository = attachmentRepository;
        _attachmentStorageService = attachmentStorageService;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Execute(IJobExecutionContext context)
        => await ProcessBatchAsync(context.CancellationToken);

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var expiredSubmissions = await _formSubmissionRepository.ListExpiredForRetentionIgnoringTenantFilterAsync(
            now,
            batchSize: 100,
            cancellationToken);

        if (expiredSubmissions.Count == 0)
        {
            return 0;
        }

        var submissionIds = expiredSubmissions.Select(s => s.Id).ToList();
        var attachments = await _attachmentRepository.ListBySubmissionIdsIgnoringTenantFilterAsync(
            submissionIds,
            cancellationToken);

        foreach (var attachment in attachments)
        {
            await _attachmentStorageService.DeleteIfExistsAsync(attachment.StoragePath, cancellationToken);
        }

        if (attachments.Count > 0)
        {
            _attachmentRepository.RemoveRange(attachments);
        }

        var attachmentsBySubmission = attachments
            .Where(a => a.FormSubmissionId.HasValue)
            .GroupBy(a => a.FormSubmissionId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var submission in expiredSubmissions)
        {
            submission.Anonymize(now);

            var deletedAttachmentCount = attachmentsBySubmission.GetValueOrDefault(submission.Id, 0);
            var auditResult = AuditLog.Create(
                submission.TenantId,
                null,
                "SUBMISSION_RETENTION_ANONYMIZED",
                "FormSubmission",
                submission.Id,
                JsonSerializer.Serialize(new
                {
                    humanTaskId = submission.HumanTaskId,
                    deletedAttachmentCount
                }),
                now);

            if (auditResult.IsSuccess)
            {
                await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return expiredSubmissions.Count;
    }
}
