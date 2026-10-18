namespace NHS111.Domain.Entities;

public class User(
    Guid id,
    string username,
    string email,
    string passwordHash,
    string role = "User",
    DateTime createdAt = default)
{
    public Guid Id { get; private set; } = id == Guid.Empty ? Guid.NewGuid() : id;
    public string Username { get; private set; } = username;
    public string Email { get; private set; } = email;
    public string PasswordHash { get; private set; } = passwordHash;
    public string Role { get; private set; } = role;
    public DateTime CreatedAt { get; private set; } = createdAt == default ? DateTime.UtcNow : createdAt;

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void UpdateRole(string newRole)
    {
        Role = newRole;
    }
}
