using FarmOS.Crew.Domain.Aggregates;

namespace FarmOS.Crew.Application;

public interface ICrewEventStore
{
    Task<Worker> LoadWorkerAsync(string workerId, CancellationToken ct);
    Task SaveWorkerAsync(Worker worker, string userId, CancellationToken ct);

    Task<Shift> LoadShiftAsync(string shiftId, CancellationToken ct);
    Task SaveShiftAsync(Shift shift, string userId, CancellationToken ct);

    Task<ApprenticeProgram> LoadApprenticeProgramAsync(string programId, CancellationToken ct);
    Task SaveApprenticeProgramAsync(ApprenticeProgram program, string userId, CancellationToken ct);
}
