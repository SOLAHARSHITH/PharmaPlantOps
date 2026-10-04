using Microsoft.AspNetCore.Authorization;
using PlantOps.Api.Contracts;
using PlantOps.Api.Data;
using PlantOps.Api.Models;

namespace PlantOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/maintenance")]
public class MaintenanceController : ControllerBase
{
    private readonly PlantOpsDbContext _db;

    public MaintenanceController(PlantOpsDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MaintenanceResponse>>> List(CancellationToken cancellationToken)
    {
        var records = await _db.MaintenanceRecords.AsNoTracking()
            .Include(record => record.Machine)
            .OrderByDescending(record => record.PerformedAtUtc)
            .ToListAsync(cancellationToken);
        return Ok(records.Select(ApiMapping.ToResponse).ToList());
    }

    [Authorize(Roles = "Supervisor")]
    [HttpPost]
    public async Task<ActionResult<MaintenanceResponse>> Create(CreateMaintenanceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest(new { error = "Description is required." });
        }

        var machine = await _db.Machines.SingleOrDefaultAsync(item => item.Id == request.MachineId, cancellationToken);
        if (machine is null)
        {
            return NotFound(new { error = "Machine was not found." });
        }

        var record = new MaintenanceRecord
        {
            MachineId = machine.Id,
            Machine = machine,
            Description = request.Description.Trim(),
            PerformedAtUtc = DateTime.UtcNow,
            PerformedBy = User.Identity?.Name ?? "unknown"
        };
        _db.MaintenanceRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/maintenance/{record.Id}", ApiMapping.ToResponse(record));
    }
}
