using Yaam.ServiceDefaults.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddYaamTelemetry("yaam-gateway");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapReverseProxy();

app.Run();
