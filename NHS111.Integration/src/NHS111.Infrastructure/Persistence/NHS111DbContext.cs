namespace NHS111.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using NHS111.Domain.Entities;

public class NHS111DbContext(DbContextOptions<NHS111DbContext> options) : DbContext(options)
{
    public DbSet<Disposition> Dispositions => Set<Disposition>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NHS111DbContext).Assembly);
    }
}
