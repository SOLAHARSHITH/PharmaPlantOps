using Microsoft.AspNetCore.Authorization;
using PlantOps.Api.Contracts;
using PlantOps.Api.Data;
using PlantOps.Api.Models;

namespace PlantOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/machines")]
public class MachinesController : ControllerBase
{
    private readonly PlantOpsDbContext _db;

    public MachinesController(PlantOpsDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MachineResponse>>> List(CancellationToken cancellationToken)
    {
        var machines = await _db.Machines.AsNoTracking().OrderBy(machine => machine.Id).ToListAsync(cancellationToken);
        var latest = await LatestTelemetry(cancellationToken);
        return Ok(machines.Select(machine => ApiMapping.ToResponse(machine, latest.GetValueOrDefault(machine.Id))).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MachineResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var machine = await _db.Machines.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (machine is null)
        {
            return NotFound();
        }

        var latest = await _db.Telemetry.AsNoTracking()
            .Where(reading => reading.MachineId == id)
            .OrderByDescending(reading => reading.TimestampUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return Ok(ApiMapping.ToResponse(machine, latest));
    }

    [HttpGet("{id:int}/telemetry")]
    public async Task<ActionResult<IReadOnlyList<TelemetryResponse>>> Telemetry(int id, CancellationToken cancellationToken)
    {
        var exists = await _db.Machines.AsNoTracking().AnyAsync(machine => machine.Id == id, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var readings = await _db.Telemetry.AsNoTracking()
            .Where(reading => reading.MachineId == id)
            .OrderByDescending(reading => reading.TimestampUtc)
            .Take(50)
            .ToListAsync(cancellationToken);
        return Ok(readings.Select(ApiMapping.ToResponse).ToList());
    }

    private async Task<Dictionary<int, TelemetryReading>> LatestTelemetry(CancellationToken cancellationToken)
    {
        var readings = await _db.Telemetry.AsNoTracking()
            .Where(reading => reading.Id == _db.Telemetry.Where(other => other.MachineId == reading.MachineId).Max(other => other.Id))
            .ToListAsync(cancellationToken);
        return readings.ToDictionary(reading => reading.MachineId);
    }
}
