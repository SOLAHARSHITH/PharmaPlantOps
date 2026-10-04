using PlantOps.Api.Models;

namespace PlantOps.Api.Data;

public class PlantOpsDbContext : DbContext
{
    public PlantOpsDbContext(DbContextOptions<PlantOpsDbContext> options) : base(options)
    {
    }

    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<TelemetryReading> Telemetry => Set<TelemetryReading>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Machine>(entity =>
        {
            entity.Property(machine => machine.Name).HasMaxLength(100);
            entity.Property(machine => machine.Line).HasMaxLength(50);
            entity.Property(machine => machine.Status).HasMaxLength(32);
        });

        modelBuilder.Entity<TelemetryReading>(entity =>
        {
            entity.ToTable("Telemetry");
            entity.Property(reading => reading.Status).HasMaxLength(32);
            entity.HasIndex(reading => new { reading.MachineId, reading.TimestampUtc });
            entity.HasOne(reading => reading.Machine)
                .WithMany(machine => machine.Telemetry)
                .HasForeignKey(reading => reading.MachineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.Property(alert => alert.Metric).HasMaxLength(32);
            entity.Property(alert => alert.Message).HasMaxLength(300);
            entity.Property(alert => alert.AcknowledgedBy).HasMaxLength(100);
            entity.HasOne(alert => alert.Machine)
                .WithMany(machine => machine.Alerts)
                .HasForeignKey(alert => alert.MachineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.Property(record => record.Description).HasMaxLength(500);
            entity.Property(record => record.PerformedBy).HasMaxLength(100);
            entity.HasOne(record => record.Machine)
                .WithMany(machine => machine.MaintenanceRecords)
                .HasForeignKey(record => record.MachineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(user => user.Username).HasMaxLength(100);
            entity.Property(user => user.Role).HasMaxLength(32);
            entity.HasIndex(user => user.Username).IsUnique();
        });
    }
}
