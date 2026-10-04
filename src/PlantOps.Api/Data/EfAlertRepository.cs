using PlantOps.Api.Models;
using PlantOps.Api.Services;

namespace PlantOps.Api.Data;

public class EfAlertRepository : IAlertRepository
{
    private readonly PlantOpsDbContext _db;

    public EfAlertRepository(PlantOpsDbContext db)
    {
        _db = db;
    }

    public Task<bool> HasOpenAlertAsync(int machineId, string metric, CancellationToken cancellationToken) =>
        _db.Alerts.AnyAsync(
            alert => alert.MachineId == machineId && alert.Metric == metric && alert.AcknowledgedAtUtc == null,
            cancellationToken);

    public async Task AddAlertAsync(Alert alert, CancellationToken cancellationToken)
    {
        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
