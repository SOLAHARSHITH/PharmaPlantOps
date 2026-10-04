using Moq;
using PlantOps.Api.Models;
using PlantOps.Api.Services;

namespace PlantOps.Api.Tests;

public class AlertDetectionServiceTests
{
    private static Machine SampleMachine() => new()
    {
        Id = 1,
        Name = "Tablet Press 1",
        MaxTemperature = 80,
        MaxPressure = 5,
        MinSpeed = 20,
        MaxSpeed = 120
    };

    private static TelemetryReading Reading(double temperature, double pressure, double speed) => new()
    {
        MachineId = 1,
        TimestampUtc = DateTime.UtcNow,
        Temperature = temperature,
        Pressure = pressure,
        Speed = speed,
        Status = "Running"
    };

    [Test]
    public async Task Breach_creates_an_alert()
    {
        var repository = new Mock<IAlertRepository>();
        repository.Setup(repo => repo.HasOpenAlertAsync(1, "Temperature", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(repo => repo.AddAlertAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = new AlertDetectionService(repository.Object);

        var alerts = await service.EvaluateAsync(SampleMachine(), Reading(90, 4, 50), CancellationToken.None);

        Assert.That(alerts, Has.Count.EqualTo(1));
        Assert.That(alerts[0].Metric, Is.EqualTo("Temperature"));
        Assert.That(alerts[0].Value, Is.EqualTo(90));
        Assert.That(alerts[0].Threshold, Is.EqualTo(80));
        repository.Verify(repo => repo.AddAlertAsync(
            It.Is<Alert>(alert => alert.Metric == "Temperature" && alert.MachineId == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task In_range_reading_does_not_create_an_alert()
    {
        var repository = new Mock<IAlertRepository>();
        var service = new AlertDetectionService(repository.Object);

        var alerts = await service.EvaluateAsync(SampleMachine(), Reading(70, 4, 50), CancellationToken.None);

        Assert.That(alerts, Is.Empty);
        repository.Verify(repo => repo.HasOpenAlertAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(repo => repo.AddAlertAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Duplicate_open_alert_is_skipped()
    {
        var repository = new Mock<IAlertRepository>();
        repository.Setup(repo => repo.HasOpenAlertAsync(1, "Temperature", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new AlertDetectionService(repository.Object);

        var alerts = await service.EvaluateAsync(SampleMachine(), Reading(90, 4, 50), CancellationToken.None);

        Assert.That(alerts, Is.Empty);
        repository.Verify(repo => repo.AddAlertAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
