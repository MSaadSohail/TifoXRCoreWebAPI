// <copyright file="Program.cs" company="Global Mobile Software LLC">
// Copyright © 2025 All Rights Reserved
// </copyright>
// <author>Syed Hussain</author>
// <date>07/28/2025</date>
// <summary>Initializes and configures the ASP.NET Core Web API application</summary>

using Microsoft.EntityFrameworkCore;
using System.Reflection;
using GMS.TifoXRCoreWebAPI.Data;
using GMS.TifoXRCoreWebAPI.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 0))));

var services = builder.Services;

// Automatically register all IRepository -> Repository mappings
var repositoryAssembly = Assembly.GetExecutingAssembly();
AppLogger.Initialize();

var typesWithInterfaces = repositoryAssembly
    .GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract)
    .Select(t => new
    {
        Implementation = t,
        Interface = t.GetInterface($"I{t.Name}")
    })
    .Where(t => t.Interface != null);

foreach (var type in typesWithInterfaces)
{
    services.AddScoped(type.Interface, type.Implementation);
}


var app = builder.Build();

app.UseMiddleware<TifoXRCoreWebAPI.Middleware.GlobalException>();

app.UseCors(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();