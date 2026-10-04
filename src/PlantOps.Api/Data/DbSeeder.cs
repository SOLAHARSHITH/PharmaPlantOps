using Microsoft.AspNetCore.Identity;
using PlantOps.Api.Models;

namespace PlantOps.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        PlantOpsDbContext db,
        IConfiguration configuration,
        IPasswordHasher<AppUser> passwordHasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(cancellationToken))
        {
            var operatorName = Required(configuration, "Auth:OperatorUsername");
            var operatorPassword = Required(configuration, "Auth:OperatorPassword");
            var supervisorName = Required(configuration, "Auth:SupervisorUsername");
            var supervisorPassword = Required(configuration, "Auth:SupervisorPassword");

            db.Users.AddRange(
                CreateUser(passwordHasher, operatorName, operatorPassword, "Operator"),
                CreateUser(passwordHasher, supervisorName, supervisorPassword, "Supervisor"));
            logger.LogInformation("Seeded operator and supervisor users");
        }

        if (await db.Machines.AnyAsync(cancellationToken))
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var now = DateTime.UtcNow;
        var machines = new[]
        {
            new Machine { Name = "Tablet Press 1", Line = "Line A", Status = "Running", MaxTemperature = 80, MaxPressure = 5.0, MinSpeed = 20, MaxSpeed = 120 },
            new Machine { Name = "Coating Pan 2", Line = "Line A", Status = "Running", MaxTemperature = 70, MaxPressure = 3.0, MinSpeed = 10, MaxSpeed = 80 },
            new Machine { Name = "Filling Line 3", Line = "Line B", Status = "Running", MaxTemperature = 25, MaxPressure = 2.5, MinSpeed = 30, MaxSpeed = 200 },
            new Machine { Name = "Autoclave 4", Line = "Line B", Status = "Idle", MaxTemperature = 130, MaxPressure = 2.0, MinSpeed = 0, MaxSpeed = 10 },
            new Machine { Name = "Packaging Unit 5", Line = "Line C", Status = "Running", MaxTemperature = 35, MaxPressure = 1.5, MinSpeed = 40, MaxSpeed = 180 }
        };

        db.Machines.AddRange(machines);
        await db.SaveChangesAsync(cancellationToken);

        db.Telemetry.AddRange(
            Reading(machines[0], now.AddMinutes(-40), 72, 3.2, 90, "Running"),
            Reading(machines[1], now.AddMinutes(-35), 61, 2.1, 40, "Running"),
            Reading(machines[2], now.AddMinutes(-30), 21, 1.8, 140, "Running"),
            Reading(machines[3], now.AddMinutes(-25), 118, 1.4, 4, "Idle"),
            Reading(machines[4], now.AddMinutes(-20), 28, 1.0, 110, "Running"));

        db.Alerts.AddRange(
            new Alert
            {
                MachineId = machines[0].Id,
                Metric = "Temperature",
                Value = 86,
                Threshold = machines[0].MaxTemperature,
                Message = $"Temperature 86 breached threshold {machines[0].MaxTemperature} on {machines[0].Name}",
                CreatedAtUtc = now.AddMinutes(-15)
            },
            new Alert
            {
                MachineId = machines[1].Id,
                Metric = "Pressure",
                Value = 3.4,
                Threshold = machines[1].MaxPressure,
                Message = $"Pressure 3.4 breached threshold {machines[1].MaxPressure} on {machines[1].Name}",
                CreatedAtUtc = now.AddHours(-2),
                AcknowledgedAtUtc = now.AddHours(-1),
                AcknowledgedBy = Required(configuration, "Auth:OperatorUsername")
            });

        db.MaintenanceRecords.AddRange(
            new MaintenanceRecord
            {
                MachineId = machines[0].Id,
                Description = "Replaced tablet press punch set",
                PerformedAtUtc = now.AddDays(-2),
                PerformedBy = Required(configuration, "Auth:SupervisorUsername")
            },
            new MaintenanceRecord
            {
                MachineId = machines[3].Id,
                Description = "Completed autoclave gasket inspection",
                PerformedAtUtc = now.AddDays(-1),
                PerformedBy = Required(configuration, "Auth:SupervisorUsername")
            });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded machines, telemetry, alerts, and maintenance records");
    }

    private static AppUser CreateUser(IPasswordHasher<AppUser> passwordHasher, string username, string password, string role)
    {
        var user = new AppUser { Username = username, Role = role };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        return user;
    }

    private static TelemetryReading Reading(Machine machine, DateTime timestamp, double temperature, double pressure, double speed, string status) =>
        new()
        {
            MachineId = machine.Id,
            TimestampUtc = timestamp,
            Temperature = temperature,
            Pressure = pressure,
            Speed = speed,
            Status = status
        };

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} must be set with an environment variable. Do not store it in source control.");
        }

        return value;
    }
}
