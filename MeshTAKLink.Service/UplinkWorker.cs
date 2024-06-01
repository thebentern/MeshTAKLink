using MeshTAKLink.Core.Converters;
using MeshTAKLink.Service.Factories;
using Meshtastic.Data;
using Meshtastic.Data.MessageFactories;
using Meshtastic.Extensions;
using Meshtastic.Protobufs;
using TheBentern.Tak.Client;

namespace MeshTAKLink.Service;

public class UplinkWorker(ILogger<UplinkWorker> logger, IConfiguration configuration, IMeshtasticConnectionFactory connectionFactory) : BackgroundService
{
    public string? TakPackagePath { get; } = configuration.GetValue<string>("TakPackagePath") ?? @"C:\Users\Meadors\Desktop\takserver.zip";

    private const int TenSeconds = 10000;
    private readonly ToRadioMessageFactory toRadioMessageFactory = new();

    public TakClient? TakClient { get; set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Uplink worker running at: {time}", DateTimeOffset.Now);

            await ConnectToTakServer();

            var connection = connectionFactory.GetConnection();
            var wantConfig = toRadioMessageFactory.CreateWantConfigMessage();

            var container = await connection.WriteToRadio(wantConfig, async (fromRadio, stateContainer) =>
            {
                // Handle incoming messages
                var takPacket = fromRadio.GetPayload<TAKPacket>();
                if (takPacket == null || takPacket.IsCompressed)
                    return false;

                await ForwardToTakServer(fromRadio, stateContainer);
                return false;
            });

            /*
                await RetryPolicyProvider.GetDeviceConnectionRetryPolicy(Logger, DoWork, (ex, __) =>
                {
                    DeviceConnection?.Disconnect();
                    TakClient?.Dispose();
                    Logger.LogError($"Encountered exception, retrying: {ex}");
                    return Task.CompletedTask;
                });
            */

            connection.Disconnect();
            TakClient?.Dispose();
            await Task.Delay(TenSeconds, stoppingToken);
        }
    }
    private async Task ConnectToTakServer()
    {
        if (TakPackagePath == null)
            return;

        TakClient = new TakClient(TakPackagePath);
        await TakClient.ConnectAsync();
    }

    private async Task ForwardToTakServer(FromRadio fromRadio, DeviceStateContainer container)
    {
        if (TakClient == null) return;

        var meshConverter = new FromRadioCotPacketConverter(fromRadio, container);
        var cot = meshConverter.Convert();
        if (cot != null)
            await TakClient.SendAsync(cot.Raw.InnerXml.Replace("<track />", String.Empty)); // Removing empty track for now
    }
}