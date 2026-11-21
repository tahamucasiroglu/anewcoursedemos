using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0012 review round 1 (I-1): uygulamanın kurduğu iki AI istemcisi (metin ve ses) yönlendirme izlemez — yerel servis
/// 307 dönerse istek başka adrese tekrar gönderilmez (öğrenci verisi yerel olmayan bir adrese taşınamaz), çağrı
/// "kullanılamıyor" (<c>http-307</c>) olur. Sahte servis bu bilgisayarda (<c>127.0.0.1</c>, rastgele kapı) çalışır.
/// </summary>
public sealed class AiRedirectTests
{
    [Fact]
    public async Task RegisteredAiClients_Redirect307_NotFollowed_Unavailable()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        ConcurrentQueue<string> paths = new();
        using CancellationTokenSource stop = new();
        Task server = ServeAsync(listener, port, paths, stop.Token);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:TextGenerator:BaseUrl"] = $"http://127.0.0.1:{port}",
                ["Ai:TextGenerator:Model"] = "gemma3:4b",
                ["Ai:SpeechToText:BaseUrl"] = $"http://127.0.0.1:{port}",
            })
            .Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider();
        CancellationToken token = TestContext.Current.CancellationToken;

        AiUnavailableException text = await Should.ThrowAsync<AiUnavailableException>(() =>
            provider.GetRequiredService<IAiTextGenerator>().GenerateAsync(new AiTextRequest("talimat", "döküm", 100), token));
        using MemoryStream audio = new([1, 2, 3, 4]);
        AiUnavailableException speech = await Should.ThrowAsync<AiUnavailableException>(() =>
            provider.GetRequiredService<ISpeechToText>().TranscribeAsync(audio, "audio/m4a", token));

        await stop.CancelAsync();
        listener.Stop();
        await server;
        text.Reason.ShouldBe("http-307");
        speech.Reason.ShouldBe("http-307");
        paths.ShouldBe(["/api/chat", "/transcribe"], ignoreOrder: true);
    }

    /// <summary>Her bağlantıda tek istek: yolu kaydeder; <c>/elsewhere</c> dışındaki her yola 307 ile oraya yönlendirir.</summary>
    private static async Task ServeAsync(TcpListener listener, int port, ConcurrentQueue<string> paths, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is OperationCanceledException or SocketException or ObjectDisposedException)
            {
                return;
            }

            using (client)
            {
                NetworkStream stream = client.GetStream();
                string head = await ReadHeadAsync(stream, cancellationToken);
                string path = head.Split(' ')[1];
                paths.Enqueue(path);
                int length = head.Split("\r\n").Where(l => l.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    .Select(l => int.Parse(l["Content-Length:".Length..].Trim(), CultureInfo.InvariantCulture)).FirstOrDefault();
                byte[] body = new byte[length];
                await stream.ReadExactlyAsync(body, cancellationToken);
                string answer = path == "/elsewhere"
                    ? "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: 2\r\nConnection: close\r\n\r\n{}"
                    : $"HTTP/1.1 307 Temporary Redirect\r\nLocation: http://127.0.0.1:{port}/elsewhere\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(answer), cancellationToken);
            }
        }
    }

    private static async Task<string> ReadHeadAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        StringBuilder head = new();
        byte[] one = new byte[1];
        while (!head.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
        {
            if (await stream.ReadAsync(one, cancellationToken) == 0)
            {
                break;
            }

            head.Append((char)one[0]);
        }

        return head.ToString();
    }
}
