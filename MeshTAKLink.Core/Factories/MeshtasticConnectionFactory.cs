using MeshTAKLink.Core.Enums;
using Meshtastic.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MeshTAKLink.Service.Factories;

public class MeshtasticConnectionFactory(ILogger logger, IConfiguration configuration) : IMeshtasticConnectionFactory
{
    private readonly ILogger logger = logger;
    private readonly IConfiguration configuration = configuration;
    private readonly static string ConnectionType = nameof(ConnectionType);

    private readonly static string Port = nameof(Port);
    private readonly static string Host = nameof(Host);
    private readonly static string Baud = nameof(Baud);

    public DeviceConnection GetConnection()
    {
        if (configuration.GetValue<ConnectionTypes>(ConnectionType) == ConnectionTypes.Serial)
            return new SerialConnection(logger, configuration.GetValue<string>(Port)!, baudRate: configuration.GetValue<int?>(Baud) ?? Meshtastic.Resources.DEFAULT_BAUD_RATE);

        else if (configuration.GetValue<ConnectionTypes>(ConnectionType) == ConnectionTypes.Tcp)
            return new TcpConnection(logger, configuration.GetValue<string>(Host) ?? "127.0.0.1", configuration.GetValue<int?>(Port) ?? Meshtastic.Resources.DEFAULT_TCP_PORT);

        throw new ApplicationException("No device connection information specified in app settings json file.");
    }
}