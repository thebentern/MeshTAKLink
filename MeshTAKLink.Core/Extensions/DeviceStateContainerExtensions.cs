using Meshtastic.Data;
using Meshtastic.Extensions;
using Meshtastic.Protobufs;

namespace MeshTAKLink.Core.Extensions;

public static class DeviceStateContainerExtensions
{
    public static IEnumerable<PacketPayload<TPayload>> GetPacketPayloads<TPayload>(this DeviceStateContainer container) where TPayload : class
    {
        return container.FromRadioMessageLog.Where(f => f.Packet != null)
            .Select(f => new PacketPayload<TPayload>(f.GetPayload<TPayload>(), f.Packet))
            .Where(p => p.HasPayload);
    }

    public static IEnumerable<MeshPacket> GetPacketsByPortnum(this DeviceStateContainer container, PortNum portNum)
    {
        return container.FromRadioMessageLog
            .Where(f => f.Packet.Decoded.Portnum == portNum)
            .Select(f => f.Packet);
    }
}
