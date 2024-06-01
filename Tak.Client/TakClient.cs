﻿using dpp.cot;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using System.IO.Compression;
using TheBentern.Tak.Client.Generated;
using TheBentern.Tak.Client.Providers;
using System.Xml.Serialization;

namespace TheBentern.Tak.Client;

public class TakClient : IDisposable
{
    private const string CoTStreamsKey = "cot_streams";
    private const string ConnectionStringKey = "connectString0";

    private readonly string packagePath;
    private readonly bool ignoreCertificateValidationIssues;
    private string? _host;

    private readonly TcpClient client = new();
    private SslStream? sslStream;

    private readonly CertificateCollectionProvider certCollectionProvider;
    private readonly XmlReaderSettings fragmentXmlReaderSettings = new() { ConformanceLevel = ConformanceLevel.Fragment };

    private bool _disposed = false;
    /// <summary>
    /// Client for connection to TAK server instance
    /// </summary>
    /// <param name="packagePath">Path to an ATAK data package zip file containing the client certificate and pref file</param>
    /// <param name="ignoreCertificateValidationIssues">Ignore any validation issues of imported client certificate</param>
    public TakClient(string packagePath, bool ignoreCertificateValidationIssues = true)
    {
        if (String.IsNullOrWhiteSpace(packagePath))
            throw new ArgumentNullException(nameof(packagePath));

        if (!File.Exists(packagePath))
            throw new ArgumentException("Package file could not be found", nameof(packagePath));

        this.packagePath = packagePath;
        this.ignoreCertificateValidationIssues = ignoreCertificateValidationIssues;
        certCollectionProvider = new CertificateCollectionProvider(packagePath);
    }

    /// <summary>
    /// Fully qualified host for TAK server instance
    /// </summary>
    public string? Host => _host;

    /// <summary>
    /// Start the connection via SSL stream to the TAK Server
    /// </summary>
    /// <returns></returns>
    public async Task ConnectAsync()
    {
        var manifest = GetPackageManifest();
        var (host, port) = GetConnectionParameters(manifest);

        var certCollection = certCollectionProvider.GetCollection(manifest);

        client.Connect(host, port);
        _host = $"{host}:{port}";
        sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(CertificateValidationCallback));

        await sslStream.AuthenticateAsClientAsync(host, certCollection, false);
    }


    /// <summary>
    /// Start the connection via SSL stream to the TAK Server and respond to received CoT events
    /// </summary>
    /// <returns></returns>
    public async Task ListenAsync(Func<CotPacket, Task> ReceivedCoTEvent, CancellationToken cancellationToken = default)
    {
        await ConnectAsync();

        while (client.Connected)
        {
            var buffer = new byte[client.ReceiveBufferSize];
            await sslStream!.ReadAsync(buffer, cancellationToken);

            foreach (Match match in Regex.Matches(Encoding.UTF8.GetString(buffer), @"<event(.|\s)*\/event>").Cast<Match>())
            {
                try
                {
                    using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(match.Value));
                    using var reader = XmlReader.Create(memStream, fragmentXmlReaderSettings);
                    while (reader.Read() && !reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "event")
                        {
                            var xml = reader.ReadOuterXml();
                            var doc = new XmlDocument();
                            doc.LoadXml(xml);

                            await ReceivedCoTEvent(new CotPacket(Event.Parse(xml), doc));
                        }
                    }
                }
                catch (Exception)
                {
                    //TODO: ILogger
                }
            }
            await Task.Delay(100, cancellationToken);
        }
    }

    /// <summary>
    /// Read CoT Events from server in buffer
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<CotPacket>> ReadEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = new List<CotPacket>();

        if (!client.Connected)
            return events;

        var buffer = new byte[client.ReceiveBufferSize];
        await sslStream!.ReadAsync(buffer, cancellationToken);

        foreach (Match match in Regex.Matches(Encoding.UTF8.GetString(buffer), @"<event(.|\s)*\/event>").Cast<Match>())
        {
            try
            {
                using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(match.Value));
                using var reader = XmlReader.Create(memStream, fragmentXmlReaderSettings);
                while (reader.Read() && !reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "event")
                    {
                        var xml = reader.ReadOuterXml();
                        var doc = new XmlDocument();
                        doc.LoadXml(xml);

                        events.Add(new CotPacket(Event.Parse(xml), doc));
                    }
                }
            }
            catch (Exception)
            {
                //TODO: ILogger
            }
        }
        return events;
    }

    /// <summary>
    /// Send CoT Message to server
    /// </summary>
    /// <param name="message">CoT Message</param>
    /// <returns></returns>
    public async Task SendAsync(Message message, CancellationToken cancellationToken = default)
    {
        await sslStream!.WriteAsync(message.ToXmlBytes(), cancellationToken);
    }

    /// <summary>
    /// Send raw CoT Message to server
    /// </summary>
    /// <param name="message">CoT Message</param>
    /// <returns></returns>
    public async Task SendAsync(string cotXml, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(cotXml);
        await sslStream!.WriteAsync(bytes, cancellationToken);
    }

    private bool CertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        return ignoreCertificateValidationIssues || sslPolicyErrors == SslPolicyErrors.None;
    }

    private Preferences GetPackageManifest()
    {
        using var package = new ZipArchive(new FileStream(packagePath!, FileMode.Open));
        var prefEntry = package.Entries.First(e => e.Name.Contains(".pref"));
        using var prefStream = prefEntry.Open();
        var xmlStream = new StreamReader(prefStream);
        XmlSerializer serializer = new(typeof(Preferences));

        return (Preferences)serializer.Deserialize(xmlStream)!;
    }

    private static (string host, int port) GetConnectionParameters(Preferences manifest)
    {
        var connection = manifest.Preference.First(p => p.Name == CoTStreamsKey)
            .Entry
            .First(e => e.Key == ConnectionStringKey);

        var connectionParams = connection.Text.Split(':');
        var host = connectionParams.First()!;
        var port = Int32.Parse(connectionParams[1]);

        return (host, port);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            sslStream?.Dispose();
            client?.Dispose();
        }
        GC.SuppressFinalize(this);
        _disposed = true;
    }

    public void Dispose() => Dispose(true);
}