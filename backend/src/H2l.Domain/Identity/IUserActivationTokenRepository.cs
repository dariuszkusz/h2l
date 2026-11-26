namespace H2l.Domain.Identity;

public interface IUserActivationTokenRepository
{
    Task<UserActivationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<UserActivationToken?> GetByTokenHashForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(UserActivationToken token, CancellationToken cancellationToken = default);
}
