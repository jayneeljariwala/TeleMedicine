namespace NHS111.Domain.Interfaces;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string token, TimeSpan? expiration = null, CancellationToken ct = default);
    Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken ct = default);
}
