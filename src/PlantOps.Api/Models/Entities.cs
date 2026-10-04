namespace PlantOps.Api.Models;

public class Machine
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Line { get; set; } = "";
    public string Status { get; set; } = "Running";
    public double MaxTemperature { get; set; }
    public double MaxPressure { get; set; }
    public double MinSpeed { get; set; }
    public double MaxSpeed { get; set; }
    public ICollection<TelemetryReading> Telemetry { get; set; } = new List<TelemetryReading>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
}

public class TelemetryReading
{
    public int Id { get; set; }
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
    public DateTime TimestampUtc { get; set; }
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public double Speed { get; set; }
    public string Status { get; set; } = "Running";
}

public class Alert
{
    public int Id { get; set; }
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
    public string Metric { get; set; } = "";
    public double Value { get; set; }
    public double Threshold { get; set; }
    public string Message { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgedBy { get; set; }
}

public class MaintenanceRecord
{
    public int Id { get; set; }
    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;
    public string Description { get; set; } = "";
    public DateTime PerformedAtUtc { get; set; }
    public string PerformedBy { get; set; } = "";
}

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
}
