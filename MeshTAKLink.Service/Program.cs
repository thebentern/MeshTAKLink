using MeshTAKLink.Service;
using MeshTAKLink.Service.Factories;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddTransient<ILogger>(s => s.GetService<ILogger<Program>>()!);
builder.Services.AddSingleton<IMeshtasticConnectionFactory, MeshtasticConnectionFactory>();
builder.Services.AddHostedService<UplinkWorker>();

var host = builder.Build();
host.Run();
