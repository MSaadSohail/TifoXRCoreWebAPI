// <copyright file="Program.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>08/19/2025</date>
// <summary>Initializes and configures the ASP.NET Core Web API application with Serilog host logging</summary>

using GMS.TifoXRCoreWebAPI.Data;
using GMS.TifoXRCoreWebAPI.Middleware;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

// Serilog
using Serilog;
using Serilog.Context;
using Serilog.Events;
using System.Data.Common;
using System.Reflection;
using GMS.TifoXRCoreWebAPI.Application.Payments;
using TifoXRCoreWebAPI.Services;
using TifoXRCoreWebAPI.Utilities.Infrastructure;
using TifoXRCoreWebAPI.Utilities.Infrastructure.Interface;
using TifoXRCoreWebAPI.Utilities.PaymentGateways;
using TifoXRCoreWebAPI.Application.Gateways;
using GMS.TifoXRCoreWebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// 0) Configure Serilog as the host logger (reads from appsettings*.json)
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithMachineName()
      .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
      .Enrich.WithProperty("ApplicationName", "TifoXRCoreWebAPI")
      .Enrich.WithProperty("Version", typeof(Program).Assembly.GetName().Version?.ToString());
});

// 1) Services
ConfigureServices(builder.Services, builder.Configuration);

// 2) Build
var app = builder.Build();

// 3) HTTP Pipeline
ConfigurePipeline(app, builder.Environment);

app.Run();

#region HELPERS

static void ConfigureServices(IServiceCollection services,  IConfiguration config)
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

    services.AddSingleton<IPayPalClientFactory, PayPalClientFactory>();

    // Gateways
    services.AddSingleton<IPaymentGateway, PaypalGateway>();
    services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();

    // Services
    services.AddScoped<IPaymentService, PaymentService>();
    services.AddScoped<IOrderService, OrderService>();

    // Factory for PayPal SDK (your existing one)
    services.AddSingleton<IPayPalClientFactory,PayPalClientFactory>();

    // ---- Auto-register repositories: I{Name} -> {Name} ----
    RegisterRepositories(services, Assembly.GetExecutingAssembly());

    // ---- CORS ----
    services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });
    services.AddSingleton(typeof(GMS.TifoXRCoreWebAPI.Utilities.Logger.Interface.IAppLogger<>), typeof(GMS.TifoXRCoreWebAPI.Utilities.Logger.AppLogger<>));

    // NOTE: Removed AppLogger.Initialize(...) — Serilog is now the host logger via UseSerilog().
}

static void ConfigurePipeline(WebApplication app, IWebHostEnvironment env)
{
    // -- CorrelationId → LogContext & response header
    app.Use(async (context, next) =>
    {
        var correlationId = GetCorrelationId(context);
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next();
        }
    });

    // -- Request Logging (Serilog) with diagnostic enrichment
    app.UseSerilogRequestLogging(opts =>
    {
        // Lean message; properties carry the rich context
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        // Add custom properties for correlation
        opts.EnrichDiagnosticContext = EnrichFromRequest;

        // Dynamically choose log level based on response + latency
        opts.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null)
                return LogEventLevel.Error;   // Exception occurred

            if (httpContext.Response.StatusCode >= 500)
                return LogEventLevel.Error;   // Server errors

            if (httpContext.Response.StatusCode >= 400)
                return LogEventLevel.Error; // Client errors

            //if (elapsed.TotalMilliseconds > 500)
            //    return LogEventLevel.Warning; // Slow requests (tune threshold)

            return LogEventLevel.Information; // Default
        };
    });


    // Global exception handling (now flows through Serilog since ILogger<T> is wired to it)
    app.UseMiddleware<GlobalException>();

    // Swagger in dev (enable in prod only if desired)
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

/// <summary>
/// Returns incoming correlation id from headers if present; otherwise uses HttpContext.TraceIdentifier.
/// </summary>
static string GetCorrelationId(HttpContext httpContext)
{
    // Accept common headers; prefer an internal one if you standardize it later
    if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var cid) && !string.IsNullOrWhiteSpace(cid))
        return cid.ToString();

    if (httpContext.Request.Headers.TryGetValue("X-Request-Id", out var rid) && !string.IsNullOrWhiteSpace(rid))
        return rid.ToString();

    return httpContext.TraceIdentifier;
}

/// <summary>
/// Adds request-scoped properties to the Serilog Diagnostic Context for the completion log.
/// </summary>
static void EnrichFromRequest(IDiagnosticContext diagnosticContext, HttpContext httpContext)
{
    // Basic HTTP context
    diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
    diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
    diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value);
    diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());

    // Normalized resource/route name if available (avoids path params noise)
    var resource = TryGetRouteName(httpContext) ?? httpContext.Request.Path.Value;
    diagnosticContext.Set("Resource", resource);
}

/// <summary>
/// Attempts to extract a stable route/resource name for logging (avoids ids in raw paths).
/// </summary>
static string? TryGetRouteName(HttpContext ctx)
{
    var endpoint = ctx.Features.Get<IEndpointFeature>()?.Endpoint ?? ctx.GetEndpoint();
    if (endpoint == null) return null;

    // Named endpoints via routing metadata
    var nameMeta = endpoint.Metadata.GetMetadata<EndpointNameMetadata>();
    if (nameMeta?.EndpointName is { Length: > 0 })
        return nameMeta.EndpointName;

    // Fallback to display name (often "HTTP: GET /api/things/{id}")
    return endpoint.DisplayName;
}

#endregion
