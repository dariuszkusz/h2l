using H2l.Domain.Common;
using H2l.Domain.Identity;
using MapsterMapper;

namespace H2l.Application.Setup.Queries;

public sealed class GetSetupStatusQueryHandler
{
    private readonly IInstanceSettingsRepository _instanceSettingsRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetSetupStatusQueryHandler(
        IInstanceSettingsRepository instanceSettingsRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _instanceSettingsRepository = instanceSettingsRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<SetupStatusResponse>> Handle(
        GetSetupStatusQuery query,
        CancellationToken cancellationToken)
    {
        var settings = await _instanceSettingsRepository.GetPrimaryAsync(cancellationToken);
        var hasAdmin = await _userRepository.AnyAdminExistsAsync(cancellationToken);

        if (settings is null)
        {
            return Result.Success(new SetupStatusResponse(false, Guid.Empty));
        }

        var mapped = _mapper.Map<SetupStatusResponse>(settings) with
        {
            IsInitialized = hasAdmin
        };

        return Result.Success(mapped);
    }
}
