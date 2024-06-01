using dpp.cot;
using System.Xml;

namespace TheBentern.Tak.Client;

public class CotPacket
{
    public CotPacket(Event @event, XmlDocument raw)
    {
        Event = @event;
        Raw = raw;
    }

    public Event Event { get; set; }
    public XmlDocument Raw { get; set; }
}
