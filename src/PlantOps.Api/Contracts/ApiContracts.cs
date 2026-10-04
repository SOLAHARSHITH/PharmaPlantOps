using System.ComponentModel.DataAnnotations;
using PlantOps.Api.Models;

namespace PlantOps.Api.Contracts;

public record LoginRequest(
    [param: Required] string Username,
    [param: Required] string Password);

public record LoginResponse(string Token, string Username, string Role);

public record TelemetryResponse(
    int Id,
    int MachineId,
    DateTime TimestampUtc,
    double Temperature,
    double Pressure,
    double Speed,
    string Status);

public record MachineResponse(
    int Id,
    string Name,
    string Line,
    string Status,
    double MaxTemperature,
    double MaxPressure,
    double MinSpeed,
    double MaxSpeed,
    TelemetryResponse? LatestTelemetry);

public record AlertResponse(
    int Id,
    int MachineId,
    string MachineName,
    string Metric,
    double Value,
    double Threshold,
    string Message,
    DateTime CreatedAtUtc,
    DateTime? AcknowledgedAtUtc,
    string? AcknowledgedBy);

public record MaintenanceResponse(
    int Id,
    int MachineId,
    string MachineName,
    string Description,
    DateTime PerformedAtUtc,
    string PerformedBy);

public record CreateMaintenanceRequest(
    int MachineId,
    [param: Required] [param: MaxLength(500)] string Description);

public record WeatherResponse(string Location, double TemperatureCelsius, int WeatherCode, string Condition);

public static class ApiMapping
{
    public static TelemetryResponse ToResponse(TelemetryReading reading) =>
        new(reading.Id, reading.MachineId, reading.TimestampUtc, reading.Temperature, reading.Pressure, reading.Speed, reading.Status);

    public static MachineResponse ToResponse(Machine machine, TelemetryReading? latest) =>
        new(
            machine.Id,
            machine.Name,
            machine.Line,
            machine.Status,
            machine.MaxTemperature,
            machine.MaxPressure,
            machine.MinSpeed,
            machine.MaxSpeed,
            latest is null ? null : ToResponse(latest));

    public static AlertResponse ToResponse(Alert alert) =>
        new(
            alert.Id,
            alert.MachineId,
            alert.Machine.Name,
            alert.Metric,
            alert.Value,
            alert.Threshold,
            alert.Message,
            alert.CreatedAtUtc,
            alert.AcknowledgedAtUtc,
            alert.AcknowledgedBy);

    public static MaintenanceResponse ToResponse(MaintenanceRecord record) =>
        new(record.Id, record.MachineId, record.Machine.Name, record.Description, record.PerformedAtUtc, record.PerformedBy);
}
