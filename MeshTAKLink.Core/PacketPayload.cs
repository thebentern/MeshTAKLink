using Meshtastic.Protobufs;

namespace MeshTAKLink.Core;

public class PacketPayload<TPayload>(TPayload? payload, MeshPacket meshPacket) where TPayload : class
{
    public TPayload? Payload { get; set; } = payload;
    public bool HasPayload => Payload != null;
    public MeshPacket MeshPacket { get; set; } = meshPacket;
}
