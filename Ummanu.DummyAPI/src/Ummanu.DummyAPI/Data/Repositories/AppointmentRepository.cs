namespace Ummanu.DummyAPI.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Ummanu.DummyAPI.Domain;

public class AppointmentRepository(UmmanuDbContext context) : IAppointmentRepository
{
    public async Task<Appointment?> GetByIdAsync(Guid id)
    {
        return await context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Appointment?> GetByDispositionIdAsync(Guid dispositionId)
    {
        return await context.Appointments.FirstOrDefaultAsync(a => a.DispositionId == dispositionId);
    }

    public async Task<IEnumerable<Appointment>> GetQueueAsync()
    {
        var appointments = await context.Appointments
            .Where(a => a.Status == AppointmentStatus.Queued)
            .ToListAsync();

        return appointments
            .OrderBy(a => GetUrgencyPriority(a.Urgency))
            .ThenBy(a => a.ReceivedAt)
            .ToList();
    }

    public async Task<int> GetNextQueuePositionAsync(string urgency)
    {
        var queuedCount = await context.Appointments
            .CountAsync(a => a.Status == AppointmentStatus.Queued);

        if (string.Equals(urgency, "Immediate", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(urgency, "UrgentSameDay", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(urgency, "Urgent", StringComparison.OrdinalIgnoreCase))
        {
            return queuedCount / 2;
        }

        return queuedCount + 1;
    }

    public async Task AddAsync(Appointment appointment)
    {
        await context.Appointments.AddAsync(appointment);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        context.Appointments.Update(appointment);
        await context.SaveChangesAsync();
    }

    private static int GetUrgencyPriority(string urgency)
    {
        if (string.Equals(urgency, "Immediate", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(urgency, "UrgentSameDay", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(urgency, "Urgent", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(urgency, "Routine", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }
}

