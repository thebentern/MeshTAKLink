using dpp.cot;

namespace TheBentern.Tak.Client.Tests;

[TestFixture]
public class PlaygroundTests
{
    [Test]
    public async Task End2End()
    {
        var takClient = new TakClient(@"C:\Users\Meadors\Downloads\takserver.zip");
        //await takClient.ListenAsync(cotCallback);
    }

    private async Task cotEventHandler(CotPacket arg)
    {
        await Task.CompletedTask;
    }
}