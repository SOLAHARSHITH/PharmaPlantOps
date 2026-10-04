using PlantOps.Api.Models;

namespace PlantOps.Api.Services;

public interface IAlertRepository
{
    Task<bool> HasOpenAlertAsync(int machineId, string metric, CancellationToken cancellationToken);
    Task AddAlertAsync(Alert alert, CancellationToken cancellationToken);
}

public record Breach(string Metric, double Value, double Threshold);

public class AlertDetectionService
{
    private readonly IAlertRepository _repository;

    public AlertDetectionService(IAlertRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Alert>> EvaluateAsync(Machine machine, TelemetryReading reading, CancellationToken cancellationToken)
    {
        var created = new List<Alert>();
        foreach (var breach in FindBreaches(machine, reading))
        {
            if (await _repository.HasOpenAlertAsync(machine.Id, breach.Metric, cancellationToken))
            {
                continue;
            }

            var alert = new Alert
            {
                MachineId = machine.Id,
                Metric = breach.Metric,
                Value = breach.Value,
                Threshold = breach.Threshold,
                Message = $"{breach.Metric} {breach.Value} breached threshold {breach.Threshold} on {machine.Name}",
                CreatedAtUtc = DateTime.UtcNow
            };
            await _repository.AddAlertAsync(alert, cancellationToken);
            created.Add(alert);
        }

        return created;
    }

    public static IReadOnlyList<Breach> FindBreaches(Machine machine, TelemetryReading reading)
    {
        var breaches = new List<Breach>();
        if (reading.Temperature > machine.MaxTemperature)
        {
            breaches.Add(new Breach("Temperature", reading.Temperature, machine.MaxTemperature));
        }

        if (reading.Pressure > machine.MaxPressure)
        {
            breaches.Add(new Breach("Pressure", reading.Pressure, machine.MaxPressure));
        }

        if (reading.Speed > machine.MaxSpeed)
        {
            breaches.Add(new Breach("Speed", reading.Speed, machine.MaxSpeed));
        }
        else if (reading.Speed < machine.MinSpeed)
        {
            breaches.Add(new Breach("Speed", reading.Speed, machine.MinSpeed));
        }

        return breaches;
    }
}
