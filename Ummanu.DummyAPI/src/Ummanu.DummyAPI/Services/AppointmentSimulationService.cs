namespace Ummanu.DummyAPI.Services;

using Microsoft.EntityFrameworkCore;
using Ummanu.DummyAPI.Data;
using Ummanu.DummyAPI.Domain;

public class AppointmentSimulationService(
    IServiceScopeFactory scopeFactory,
    ILogger<AppointmentSimulationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AppointmentSimulationService started.");
        var tickCount = 0;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            tickCount++;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<UmmanuDbContext>();

                // Every 5 seconds: find oldest Queued appointment
                // -> Assign a random clinician GUID
                // -> Set Status = InConsultation
                var oldestQueued = await dbContext.Appointments
                    .Where(a => a.Status == AppointmentStatus.Queued)
                    .OrderBy(a => a.ReceivedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (oldestQueued != null)
                {
                    oldestQueued.AssignedClinicianId = Guid.NewGuid();
                    oldestQueued.Status = AppointmentStatus.InConsultation;
                    oldestQueued.UpdatedAt = DateTime.UtcNow;
                    dbContext.Appointments.Update(oldestQueued);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    logger.LogInformation(
                        "Simulation: Appointment {Id} moved to InConsultation with clinician {ClinicianId}",
                        oldestQueued.Id, oldestQueued.AssignedClinicianId);
                }

                // Every 10 seconds: find oldest InConsultation appointment
                // -> Set Status = Completed
                if (tickCount % 2 == 0)
                {
                    var oldestInConsultation = await dbContext.Appointments
                        .Where(a => a.Status == AppointmentStatus.InConsultation)
                        .OrderBy(a => a.UpdatedAt)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (oldestInConsultation != null)
                    {
                        oldestInConsultation.Status = AppointmentStatus.Completed;
                        oldestInConsultation.UpdatedAt = DateTime.UtcNow;
                        dbContext.Appointments.Update(oldestInConsultation);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        logger.LogInformation(
                            "Simulation: Appointment {Id} moved to Completed",
                            oldestInConsultation.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during appointment simulation cycle.");
            }
        }

        logger.LogInformation("AppointmentSimulationService stopped.");
    }
}

