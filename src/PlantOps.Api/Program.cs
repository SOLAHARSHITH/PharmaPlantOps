using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PlantOps.Api.Data;
using PlantOps.Api.Models;
using PlantOps.Api.Options;
using PlantOps.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var authSection = builder.Configuration.GetSection("Auth");
var jwtKey = authSection["JwtSigningKey"] ?? "";
var jwtIssuer = authSection["JwtIssuer"] ?? "PlantOps";
var jwtAudience = authSection["JwtAudience"] ?? "PlantOps";
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("Auth:JwtSigningKey must be at least 32 characters. Set it with an environment variable, not in source control.");
}

builder.Services.Configure<AuthOptions>(authSection);
builder.Services.Configure<SimulatorOptions>(builder.Configuration.GetSection("Simulator"));
builder.Services.Configure<WeatherOptions>(builder.Configuration.GetSection("Weather"));

var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

var databaseProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";
builder.Services.AddDbContext<PlantOpsDbContext>(options =>
{
    if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(builder.Configuration["Database:Sqlite"] ?? "Data Source=plantops.db");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("PlantOps"));
    }
});

builder.Services.AddHealthChecks().AddDbContextCheck<PlantOpsDbContext>("sql");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var corsOrigin = builder.Configuration["Cors:Origin"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.WithOrigins(corsOrigin).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IAlertRepository, EfAlertRepository>();
builder.Services.AddScoped<AlertDetectionService>();
builder.Services.AddHttpClient<IWeatherClient, OpenMeteoWeatherClient>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PlantOps/1.0");
});
builder.Services.AddHostedService<TelemetrySimulatorJob>();

var app = builder.Build();
await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, app.Logger);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString()
            })
        };
        await context.Response.WriteAsJsonAsync(payload);
    }
}).AllowAnonymous();

app.Run();

public partial class Program;
