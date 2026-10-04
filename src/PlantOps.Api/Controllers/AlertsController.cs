using Microsoft.AspNetCore.Authorization;
using PlantOps.Api.Contracts;
using PlantOps.Api.Data;

namespace PlantOps.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private readonly PlantOpsDbContext _db;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(PlantOpsDbContext db, ILogger<AlertsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AlertResponse>>> List(CancellationToken cancellationToken)
    {
        var alerts = await _db.Alerts.AsNoTracking()
            .Include(alert => alert.Machine)
            .OrderByDescending(alert => alert.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return Ok(alerts.Select(ApiMapping.ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AlertResponse>> Get(int id, CancellationToken cancellationToken)
    {
        var alert = await _db.Alerts.AsNoTracking()
            .Include(item => item.Machine)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return alert is null ? NotFound() : Ok(ApiMapping.ToResponse(alert));
    }

    [HttpPost("{id:int}/acknowledge")]
    public async Task<ActionResult<AlertResponse>> Acknowledge(int id, CancellationToken cancellationToken)
    {
        var alert = await _db.Alerts.Include(item => item.Machine).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (alert is null)
        {
            return NotFound();
        }

        if (alert.AcknowledgedAtUtc is null)
        {
            alert.AcknowledgedAtUtc = DateTime.UtcNow;
            alert.AcknowledgedBy = User.Identity?.Name ?? "unknown";
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Alert {AlertId} acknowledged by {User}", alert.Id, alert.AcknowledgedBy);
        }

        return Ok(ApiMapping.ToResponse(alert));
    }
}
