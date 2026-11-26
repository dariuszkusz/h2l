using H2l.Application.Auth.DTOs;
using H2l.Application.Common;
using H2l.Domain.Common;
using H2l.Domain.Identity;
using MapsterMapper;

namespace H2l.Application.Users.Queries;

public sealed class GetCurrentUserProfileQueryHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetCurrentUserProfileQueryHandler(
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<UserProfileResponse>> Handle(
        GetCurrentUserProfileQuery query,
        CancellationToken cancellationToken)
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.UserId is null)
        {
            return Result.Failure<UserProfileResponse>(
                Error.Unauthorized("UNAUTHORIZED", "Authentication is required."));
        }

        var user = await _userRepository.GetByIdAsync(_currentUserContext.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<UserProfileResponse>(
                Error.Unauthorized("USER_NOT_FOUND", "Authenticated user was not found."));
        }

        return Result.Success(_mapper.Map<UserProfileResponse>(user));
    }
}
