using H2l.Application.Common;
using H2l.Domain.Common;
using H2l.Domain.Identity;

namespace H2l.Application.Users.Commands;

public sealed class UpdateUserPreferencesCommandHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserPreferencesCommandHandler(
        ICurrentUserContext currentUserContext,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateUserPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.UserId is null)
        {
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "Authentication is required."));
        }

        var user = await _userRepository.GetByIdForUpdateAsync(_currentUserContext.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.Unauthorized("USER_NOT_FOUND", "Authenticated user was not found."));
        }

        var updateResult = user.UpdatePreferences(command.PreferredLanguage, command.PreferredTheme);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
