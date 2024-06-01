using Meshtastic.Connections;

namespace MeshTAKLink.Service.Factories;

public interface IMeshtasticConnectionFactory
{
    DeviceConnection GetConnection();
}