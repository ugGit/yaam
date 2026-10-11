using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Yaam.API;
using Yaam.Infrastructure;
using Yaam.Infrastructure.Messaging;
using Yaam.ServiceDefaults.Messaging;
using Yaam.ServiceDefaults.Telemetry;
using Yaam.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.AddYaamTelemetry("yaam-api");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required."),
    builder.Configuration);
builder.Services.AddYaamAuthentication(builder.Configuration);
builder.Services.AddYaamRabbitMq(builder.Configuration);
builder.Services.Configure<OutboxSettings>(builder.Configuration.GetSection("Outbox"));
builder.Services.AddSingleton<OutboxMetrics>();
builder.Services.AddHostedService<OutboxRelay>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
