namespace Ummanu.DummyAPI.Data.Repositories;

using Ummanu.DummyAPI.Domain;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id);
    Task<Appointment?> GetByDispositionIdAsync(Guid dispositionId);
    Task<IEnumerable<Appointment>> GetQueueAsync();
    Task<int> GetNextQueuePositionAsync(string urgency);
    Task AddAsync(Appointment appointment);
    Task UpdateAsync(Appointment appointment);
}

