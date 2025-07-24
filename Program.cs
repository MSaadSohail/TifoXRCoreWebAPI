using Microsoft.EntityFrameworkCore;
using System.Reflection;
using TifoXRCoreWebAPI.Data;


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

app.UseCors(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();