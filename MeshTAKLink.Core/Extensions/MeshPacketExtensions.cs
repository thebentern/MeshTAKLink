using Meshtastic.Protobufs;

namespace MeshTAKLink.Core.Extensions;

public static class MeshPacketExtensions
{
    public static DateTime ReceivedAt(this MeshPacket packet)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(packet.RxTime);
        return dateTime.ToLocalTime();
    }
}
