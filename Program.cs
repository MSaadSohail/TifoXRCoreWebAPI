// <copyright file="Program.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/14/2025</date>
// <summary>Initializes and configures the ASP.NET Core Web API application</summary>

using GMS.TifoXRCoreWebAPI.Data;
using GMS.TifoXRCoreWebAPI.Middleware;
using GMS.TifoXRCoreWebAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data.Common;
using System.Reflection;
using TifoXRCoreWebAPI.Utilities.Infrastructure;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;

var builder = WebApplication.CreateBuilder(args);

// 1) Services
ConfigureServices(builder.Services, builder.Configuration);

// 2) Build
var app = builder.Build();

// 3) HTTP Pipeline
ConfigurePipeline(app, builder.Environment);

app.Run();


#region HELPERS

static void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    // MVC + Swagger
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    // ---- Database: EF Core (Pomelo/MySqlConnector) ----
    var dsn = config.GetConnectionString("DefaultConnection")
             ?? config["Database:ConnectionString"]               // optional fallback shape
             ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");

    services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(dsn, ServerVersion.AutoDetect(dsn)));

    // ---- Provider-agnostic wrapper (DbProviderFactories) ----
    var providerInvariant = config["Database:ProviderInvariantName"] ?? "MySqlConnector";

    DbProviderFactories.RegisterFactory("MySqlConnector", MySqlConnectorFactory.Instance);

    services.AddSingleton(new DbProviderOptions
    {
        ProviderInvariantName = providerInvariant,
        ConnectionString = dsn
    });

    services.AddSingleton<IDbProvider, DefaultDbProvider>();
    services.AddSingleton<ISqlDialect>(_ =>
        providerInvariant.Contains("SqlClient", StringComparison.OrdinalIgnoreCase)
            ? new SqlServerDialect()
            : new MySqlDialect());

    // ---- Auto-register repositories: I{Name} -> {Name} ----
    RegisterRepositories(services, Assembly.GetExecutingAssembly());

    // ---- CORS ----
    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    // ---- Logging ----
    var appInsightsConnectionString = config["ApplicationInsights:ConnectionString"];

    // Automatically register all IRepository -> Repository mappings
    var repositoryAssembly = Assembly.GetExecutingAssembly();

    // Initialize AppLogger
    AppLogger.Initialize(appInsightsConnectionString ?? string.Empty);
}

static void ConfigurePipeline(WebApplication app, IWebHostEnvironment env)
{
    // Global exception handling
    app.UseMiddleware<GlobalException>();

    // Swagger in dev (enable in prod only)
    if (env.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowAll");

    // later add auth:
    // app.UseAuthentication();
    // app.UseAuthorization();

    app.MapControllers();
}

static void RegisterRepositories(IServiceCollection services, Assembly assembly)
{
    var pairs = assembly
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Repository", StringComparison.Ordinal))
        .Select(t => new { Implementation = t, Interface = t.GetInterface($"I{t.Name}") })
        .Where(x => x.Interface != null);

    foreach (var p in pairs)
        services.AddScoped(p.Interface!, p.Implementation);
}

#endregion