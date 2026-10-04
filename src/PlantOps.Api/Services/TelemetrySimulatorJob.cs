using Microsoft.Extensions.Options;
using PlantOps.Api.Data;
using PlantOps.Api.Models;
using PlantOps.Api.Options;

namespace PlantOps.Api.Services;

public class TelemetrySimulatorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SimulatorOptions _options;
    private readonly ILogger<TelemetrySimulatorJob> _logger;
    private int _tick;

    public TelemetrySimulatorJob(
        IServiceScopeFactory scopeFactory,
        IOptions<SimulatorOptions> options,
        ILogger<TelemetrySimulatorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Telemetry simulator is disabled");
            return;
        }

        _logger.LogInformation("Telemetry simulator started with a {IntervalSeconds}s interval", _options.IntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SimulateOnce(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Telemetry simulator iteration failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SimulateOnce(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlantOpsDbContext>();
        var detector = scope.ServiceProvider.GetRequiredService<AlertDetectionService>();
        var machines = await db.Machines.OrderBy(machine => machine.Id).ToListAsync(cancellationToken);
        if (machines.Count == 0)
        {
            return;
        }

        var tick = Interlocked.Increment(ref _tick);
        var breachIndex = (tick - 1) % machines.Count;

        for (var index = 0; index < machines.Count; index++)
        {
            var machine = machines[index];
            var breach = index == breachIndex;
            var reading = new TelemetryReading
            {
                MachineId = machine.Id,
                TimestampUtc = DateTime.UtcNow,
                Temperature = breach ? machine.MaxTemperature + 5 : Math.Max(0, machine.MaxTemperature - 15),
                Pressure = Math.Max(0, machine.MaxPressure - 0.5),
                Speed = (machine.MinSpeed + machine.MaxSpeed) / 2,
                Status = breach ? "Warning" : "Running"
            };

            db.Telemetry.Add(reading);
            await db.SaveChangesAsync(cancellationToken);

            var raised = await detector.EvaluateAsync(machine, reading, cancellationToken);
            machine.Status = AlertDetectionService.FindBreaches(machine, reading).Count > 0 ? "Warning" : "Running";
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Stored telemetry for {MachineName}: temperature {Temperature}, pressure {Pressure}, speed {Speed}",
                machine.Name,
                reading.Temperature,
                reading.Pressure,
                reading.Speed);

            foreach (var alert in raised)
            {
                _logger.LogWarning("Raised {Metric} alert {AlertId} for {MachineName}", alert.Metric, alert.Id, machine.Name);
            }
        }
    }
}
