namespace NHS111.Domain.Interfaces;

using NHS111.Domain.Entities;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
