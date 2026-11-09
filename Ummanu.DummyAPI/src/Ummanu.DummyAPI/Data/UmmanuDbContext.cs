namespace Ummanu.DummyAPI.Data;

using Microsoft.EntityFrameworkCore;
using Ummanu.DummyAPI.Domain;

public class UmmanuDbContext(DbContextOptions<UmmanuDbContext> options) : DbContext(options)
{
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.DispositionId).IsUnique();

            entity.Property(a => a.NhsNumber).HasMaxLength(20).IsRequired();
            entity.Property(a => a.PatientName).HasMaxLength(200).IsRequired();
            entity.Property(a => a.DxCode).HasMaxLength(10).IsRequired();
            entity.Property(a => a.DosCode).HasMaxLength(10).IsRequired();
            entity.Property(a => a.Urgency).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Status).HasConversion<int>().IsRequired();
            entity.Property(a => a.Notes).HasMaxLength(1000);
        });
    }
}

