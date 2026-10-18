namespace NHS111.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using NHS111.Domain.Entities;
using NHS111.Domain.Interfaces;

public class UserRepository(NHS111DbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await dbContext.Users.FirstOrDefaultAsync(
            u => u.Username.ToLower() == username.ToLower(), ct);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await dbContext.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await dbContext.Users.AddAsync(user, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(ct);
    }
}
