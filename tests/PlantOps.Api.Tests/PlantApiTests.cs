using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlantOps.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseFile = Path.Combine(Path.GetTempPath(), $"plantops-{Guid.NewGuid():N}.db");

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("Database__Sqlite", $"Data Source={_databaseFile}");
        Environment.SetEnvironmentVariable("Simulator__Enabled", "false");
        Environment.SetEnvironmentVariable("Auth__JwtIssuer", "PlantOps");
        Environment.SetEnvironmentVariable("Auth__JwtAudience", "PlantOps");
        Environment.SetEnvironmentVariable("Auth__JwtSigningKey", "test-signing-key-at-least-32-characters");
        Environment.SetEnvironmentVariable("Auth__OperatorUsername", "operator");
        Environment.SetEnvironmentVariable("Auth__OperatorPassword", "Operator123!");
        Environment.SetEnvironmentVariable("Auth__SupervisorUsername", "supervisor");
        Environment.SetEnvironmentVariable("Auth__SupervisorPassword", "Supervisor123!");
    }

    public string DatabaseFile => _databaseFile;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        TryDelete(_databaseFile);
        TryDelete(_databaseFile + "-wal");
        TryDelete(_databaseFile + "-shm");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
    }
}

public class PlantApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void Start()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void Stop()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Health_is_healthy()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("Healthy"));
    }

    [Test]
    public async Task Login_rejects_a_bad_password()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username = "operator", password = "wrong-password" });
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), body);
    }

    [Test]
    public async Task Machines_require_a_token_and_then_return_seed_data()
    {
        var anonymous = await _client.GetAsync("/api/machines");
        Assert.That(anonymous.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var token = await LoginAsync("operator", "Operator123!");
        var response = await SendAsync(HttpMethod.Get, "/api/machines", token);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var machines = await response.Content.ReadFromJsonAsync<List<MachineItem>>(JsonOptions);
        Assert.That(machines, Is.Not.Null);
        Assert.That(machines!, Has.Count.EqualTo(5));
        Assert.That(machines!.Any(machine => machine.Name == "Tablet Press 1"), Is.True);
    }

    [Test]
    public async Task Operator_can_acknowledge_an_open_alert()
    {
        var token = await LoginAsync("operator", "Operator123!");
        var listResponse = await SendAsync(HttpMethod.Get, "/api/alerts", token);
        var alerts = await listResponse.Content.ReadFromJsonAsync<List<AlertItem>>(JsonOptions);
        var open = alerts!.First(alert => alert.AcknowledgedAtUtc is null);

        var response = await SendAsync(HttpMethod.Post, $"/api/alerts/{open.Id}/acknowledge", token);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var acknowledged = await response.Content.ReadFromJsonAsync<AlertItem>(JsonOptions);
        Assert.That(acknowledged!.AcknowledgedBy, Is.EqualTo("operator"));
        Assert.That(acknowledged.AcknowledgedAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task Operator_cannot_create_maintenance_and_supervisor_can()
    {
        var operatorToken = await LoginAsync("operator", "Operator123!");
        var machinesResponse = await SendAsync(HttpMethod.Get, "/api/machines", operatorToken);
        var machines = await machinesResponse.Content.ReadFromJsonAsync<List<MachineItem>>(JsonOptions);
        var machineId = machines![0].Id;

        var forbidden = await SendAsync(HttpMethod.Post, "/api/maintenance", operatorToken, new { machineId, description = "Operator attempt" });
        Assert.That(forbidden.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        var supervisorToken = await LoginAsync("supervisor", "Supervisor123!");
        var description = $"Belt inspection {Guid.NewGuid():N}";
        var created = await SendAsync(HttpMethod.Post, "/api/maintenance", supervisorToken, new { machineId, description });
        Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await created.Content.ReadFromJsonAsync<MaintenanceItem>(JsonOptions);
        Assert.That(body!.Description, Is.EqualTo(description));
        Assert.That(body.PerformedBy, Is.EqualTo("supervisor"));
    }

    private async Task<string> LoginAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginItem>(JsonOptions);
        return body!.Token;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private sealed record LoginItem(string Token, string Username, string Role);
    private sealed record MachineItem(int Id, string Name);
    private sealed record AlertItem(int Id, DateTime? AcknowledgedAtUtc, string? AcknowledgedBy);
    private sealed record MaintenanceItem(string Description, string PerformedBy);
}
