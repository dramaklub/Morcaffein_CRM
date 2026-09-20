using CRM_Morcaffein.api.Services;
using CRM_Morcaffein.domain.entities;
using CRM_Morcaffein.infrastructure.data;
using CRM_Morcaffein.infrastructure.services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// LocalDB named instances (anything other than the reserved "MSSQLLocalDB" default) are
// NOT auto-started on a cold connection the way the default instance is — a fresh
// connection attempt just fails with "the specified LocalDB instance does not exist"
// unless something has already started it. Ensure it's running before EF ever tries to
// connect, so this doesn't depend on remembering a manual "sqllocaldb start" step.
EnsureLocalDbInstanceStarted(builder.Configuration.GetConnectionString("LocalMasterCrm"));

static void EnsureLocalDbInstanceStarted(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString)) return;

    var match = Regex.Match(connectionString, @"\(localdb\)\\([^;]+)", RegexOptions.IgnoreCase);
    if (!match.Success) return;

    var instanceName = match.Groups[1].Value;

    try
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "sqllocaldb",
            Arguments = $"start \"{instanceName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        process?.WaitForExit(10_000);
    }
    catch
    {
        // Best-effort — if sqllocaldb.exe isn't on PATH or the instance is already
        // running, the subsequent EF connection attempt will surface any real problem.
    }
}

builder.Services.AddDbContext<MasterCrmDbContext>(options =>
  options.UseSqlServer(
    builder.Configuration.GetConnectionString("LocalMasterCrm")));

builder.Services.AddDbContext<TenantCrmDbContext>(options =>
  options.UseSqlServer(
    builder.Configuration.GetConnectionString("LocalTenantCrm")));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
var jwtIssuer = jwtSection["Issuer"] ?? "MorcaffeinApi";
var jwtAudience = jwtSection["Audience"] ?? "MorcaffeinClient";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero   // no grace period on expiry — 8h means 8h
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddOpenApi();

builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapPost("/companies", async (
    
    Company company,
    
    MasterCrmDbContext db) =>
{

    db.Companies.Add(company);

    await db.SaveChangesAsync();

    return Results.Created($"/companies/{company.CompanyId}", company);
});

app.MapPost("/devices", async (

  Device device,

  MasterCrmDbContext db) =>

{

    db.Devices.Add(device);

    await db.SaveChangesAsync();



    return Results.Created($"/devices/{device.DeviceId}", device);

});

app.MapPost("/company-databases", async (

  CompanyDatabase companyDatabase,

  MasterCrmDbContext db) =>

{

    db.CompanyDatabases.Add(companyDatabase);

    await db.SaveChangesAsync();



    return Results.Created(

      $"/company-databases/{companyDatabase.CompanyDatabaseId}",

      companyDatabase);

});

app.MapGet("/test-tenant/{companyId:int}", async (
    int companyId,
    ITenantDbContextFactory tenantFactory) =>
{
    await using var tenantDb = await tenantFactory.CreateAsync(companyId);

    var customerCount = await tenantDb.Customers.CountAsync();

    return Results.Ok(new
    {
        companyId,
        customerCount
    });
});

app.Run();
