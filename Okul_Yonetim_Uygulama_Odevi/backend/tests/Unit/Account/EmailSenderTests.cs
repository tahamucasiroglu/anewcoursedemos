using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Email;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Account;

/// <summary>
/// Spec 0006 AC-9, AC-10: SMTP ayarlı değilken gönderilmez ve yalnızca kişi kimliğiyle "ayarlı değil" logu; ayarlıyken
/// sahte SMTP sunucusuna Türkçe konu/gövdeyle gider; ulaşılamayan sunucu üst sınır içinde biter; geliştirme klasörü
/// yalnızca Development'ta; kuyruk gönderim hatasında uyarı loglar, çökmez.
/// </summary>
public sealed class EmailSenderTests
{
    private static readonly EmailMessage Message = new("veli@ornek.test", "Ayşe Veli", "Okul Yönetim — şifre yenileme", "Merhaba\nhttp://okul/#/sifre-yenile?anahtar=GIZLI-ANAHTAR-123\n", Guid.Parse("11111111-1111-4111-8111-111111111111"));

    [Fact]
    public async Task NotConfigured_SendsNothing_LogsOnlyUserId()
    {
        ListLogger<EmailSender> log = new();
        EmailSender sender = new(Options.Create(new EmailOptions()), new Env("Production", Path.GetTempPath()), log);

        await sender.SendAsync(Message, TestContext.Current.CancellationToken);

        string text = string.Join('\n', log.Lines);
        text.ShouldContain("E-posta gönderilemedi: SMTP ayarlı değil");
        text.ShouldContain(Message.UserId!.Value.ToString());
        text.ShouldNotContain("veli@ornek.test");
        text.ShouldNotContain("GIZLI-ANAHTAR-123");
    }

    [Fact]
    public async Task Configured_DeliversToSmtpServer_WithTurkishSubjectAndBody()
    {
        using FakeSmtpServer server = new();
        EmailOptions options = new() { Host = "127.0.0.1", Port = server.Port, UseStartTls = false, FromAddress = "okul@ornek.test", FromName = "Okul Yönetim", WebBaseUrl = "http://okul" };
        ListLogger<EmailSender> log = new();
        EmailSender sender = new(Options.Create(options), new Env("Production", Path.GetTempPath()), log);

        await sender.SendAsync(Message, TestContext.Current.CancellationToken);

        string data = await server.ReceivedAsync();
        data.ShouldContain("RCPT TO:<veli@ornek.test>");
        data.ShouldContain("MAIL FROM:<okul@ornek.test>");
        string decoded = DecodeHeaders(data);
        decoded.ShouldContain("Okul Yönetim — şifre yenileme");
        decoded.ShouldMatch("(?m)^From: .*Okul Yönetim.*<okul@ornek.test>");
        DecodeBody(data).ShouldContain("http://okul/#/sifre-yenile?anahtar=GIZLI-ANAHTAR-123");
        string.Join('\n', log.Lines).ShouldNotContain("GIZLI-ANAHTAR-123");
    }

    /// <summary>Review F-2: bağlantı ayardaki web adresiyle başlar; anahtar "#" sonrasında ve kodlanmış.</summary>
    [Theory]
    [InlineData("http://okul:8080/", "http://okul:8080/#/sifre-yenile?anahtar=a%2Bb")]
    [InlineData("https://okul.example", "https://okul.example/#/sifre-yenile?anahtar=a%2Bb")]
    public void PasswordResetLink_StartsWithConfiguredWebAddress(string webBaseUrl, string expected) =>
        new AppLinks(Options.Create(new EmailOptions { WebBaseUrl = webBaseUrl })).PasswordReset("a+b").ShouldBe(expected);

    /// <summary>Review F-2: bağlantıyı kabul edip hiç yanıt vermeyen sunucu — gönderim ayar süresi civarında biter.</summary>
    [Fact]
    public async Task SilentServer_GivesUpAfterTimeout()
    {
        TcpListener silent = new(IPAddress.Loopback, 0);
        silent.Start();
        Task<TcpClient> accepted = silent.AcceptTcpClientAsync(TestContext.Current.CancellationToken).AsTask();
        int port = ((IPEndPoint)silent.LocalEndpoint).Port;
        EmailOptions options = new() { Host = "127.0.0.1", Port = port, UseStartTls = false, FromAddress = "okul@ornek.test", WebBaseUrl = "http://okul", TimeoutSeconds = 2, UserName = "kullanici", Password = "GIZLI-SMTP-SIFRE" };
        ListLogger<BackgroundEmailOutbox> log = new();
        EmailSender sender = new(Options.Create(options), new Env("Production", Path.GetTempPath()), new ListLogger<EmailSender>());
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        await Should.ThrowAsync<Exception>(() => sender.SendAsync(Message, TestContext.Current.CancellationToken));

        watch.Elapsed.ShouldBeInRange(TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(8));
        using BackgroundEmailOutbox outbox = new(sender, log);
        await outbox.StartAsync(TestContext.Current.CancellationToken);
        outbox.Enqueue(Message);
        for (int i = 0; i < 200 && !log.Lines.Any(l => l.Contains("gönderilemedi", StringComparison.Ordinal)); i++)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        await outbox.StopAsync(TestContext.Current.CancellationToken);
        string text = string.Join('\n', log.Lines);
        text.ShouldContain("E-posta gönderilemedi");
        text.ShouldNotContain("GIZLI-SMTP-SIFRE");
        silent.Stop();
        (await accepted).Dispose();
    }

    [Fact]
    public async Task UnreachableServer_FailsWithinTimeout()
    {
        TcpListener probe = new(IPAddress.Loopback, 0);
        probe.Start();
        int closedPort = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        EmailOptions options = new() { Host = "127.0.0.1", Port = closedPort, UseStartTls = false, FromAddress = "okul@ornek.test", WebBaseUrl = "http://okul", TimeoutSeconds = 2 };
        EmailSender sender = new(Options.Create(options), new Env("Production", Path.GetTempPath()), new ListLogger<EmailSender>());
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        await Should.ThrowAsync<Exception>(() => sender.SendAsync(Message, TestContext.Current.CancellationToken));

        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    [InlineData("Testing", false)]
    public async Task PickupDirectory_OnlyInDevelopment(string environment, bool written)
    {
        string root = Path.Combine(Path.GetTempPath(), "okul-mail-test-" + Guid.NewGuid().ToString("N"));
        EmailOptions options = new() { PickupDirectory = "mail-outbox", FromAddress = "okul@localhost", WebBaseUrl = "http://localhost:5173" };
        EmailSender sender = new(Options.Create(options), new Env(environment, root), new ListLogger<EmailSender>());

        await sender.SendAsync(Message, TestContext.Current.CancellationToken);

        string folder = Path.Combine(root, "mail-outbox");
        (Directory.Exists(folder) && Directory.GetFiles(folder).Length > 0).ShouldBe(written);
    }

    [Fact]
    public async Task Outbox_SendFailure_IsLoggedAsWarning_AndNextMessageStillSent()
    {
        FlakySender flaky = new();
        ListLogger<BackgroundEmailOutbox> log = new();
        using BackgroundEmailOutbox outbox = new(flaky, log);
        await outbox.StartAsync(TestContext.Current.CancellationToken);

        outbox.Enqueue(Message);
        outbox.Enqueue(Message with { UserId = Guid.Parse("22222222-2222-4222-8222-222222222222") });
        for (int i = 0; i < 100 && flaky.Sent < 1; i++)
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        await outbox.StopAsync(TestContext.Current.CancellationToken);
        flaky.Sent.ShouldBe(1);
        string text = string.Join('\n', log.Lines);
        text.ShouldContain("E-posta gönderilemedi");
        text.ShouldNotContain("GIZLI-ANAHTAR-123");
        text.ShouldNotContain("veli@ornek.test");
    }

    private static string DecodeHeaders(string data)
    {
        // "=?utf-8?B?...?=" kodlu konu satırlarını çöz.
        string b = System.Text.RegularExpressions.Regex.Replace(data, @"=\?utf-8\?B\?([^?]+)\?=", m => Encoding.UTF8.GetString(Convert.FromBase64String(m.Groups[1].Value)), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return System.Text.RegularExpressions.Regex.Replace(b, @"=\?utf-8\?Q\?([^?]+)\?=", m => DecodeQ(m.Groups[1].Value), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string DecodeQ(string value)
    {
        List<byte> bytes = [];
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '=' && i + 2 < value.Length)
            {
                bytes.Add(Convert.ToByte(value.Substring(i + 1, 2), 16));
                i += 2;
            }
            else
            {
                bytes.Add((byte)(value[i] == '_' ? ' ' : value[i]));
            }
        }

        return Encoding.UTF8.GetString([.. bytes]);
    }

    private static string DecodeBody(string data)
    {
        string[] parts = data.Split(["\r\n\r\n", "\n\n"], 2, StringSplitOptions.None);
        string headers = parts[0];
        string rest = parts.Length > 1 ? parts[1] : string.Empty;
        string body = string.Join("\r\n", rest.Split('\n').Select(l => l.TrimEnd('\r')).TakeWhile(l => l != "."));
        if (headers.Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(string.Concat(body.Where(c => !char.IsWhiteSpace(c)))));
        }

        if (headers.Contains("quoted-printable", StringComparison.OrdinalIgnoreCase))
        {
            string joined = System.Text.RegularExpressions.Regex.Replace(body, "=\r?\n", string.Empty);
            List<byte> bytes = [];
            for (int i = 0; i < joined.Length; i++)
            {
                if (joined[i] == '=' && i + 2 < joined.Length && Uri.IsHexDigit(joined[i + 1]) && Uri.IsHexDigit(joined[i + 2]))
                {
                    bytes.Add(Convert.ToByte(joined.Substring(i + 1, 2), 16));
                    i += 2;
                }
                else
                {
                    bytes.AddRange(Encoding.UTF8.GetBytes(joined[i].ToString()));
                }
            }

            return Encoding.UTF8.GetString([.. bytes]);
        }

        return body;
    }

    private sealed class FlakySender : IEmailSender
    {
        private int calls;

        public int Sent { get; private set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref this.calls) == 1)
            {
                throw new InvalidOperationException("sunucu reddetti");
            }

            this.Sent++;
            return Task.CompletedTask;
        }
    }

    private sealed class Env : IHostEnvironment
    {
        public Env(string name, string root)
        {
            this.EnvironmentName = name;
            this.ContentRootPath = root;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "test";

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (this.Lines)
            {
                this.Lines.Add(formatter(state, exception));
            }
        }
    }

    /// <summary>En küçük SMTP sunucusu: komutları kabul eder, konuşmanın tamamını saklar.</summary>
    private sealed class FakeSmtpServer : IDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly Task<string> conversation;

        public FakeSmtpServer()
        {
            this.listener.Start();
            this.Port = ((IPEndPoint)this.listener.LocalEndpoint).Port;
            this.conversation = this.RunAsync();
        }

        public int Port { get; }

        public Task<string> ReceivedAsync() => this.conversation.WaitAsync(TimeSpan.FromSeconds(10));

        public void Dispose() => this.listener.Stop();

        private async Task<string> RunAsync()
        {
            using TcpClient client = await this.listener.AcceptTcpClientAsync();
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream, Encoding.UTF8);
            using StreamWriter writer = new(stream, Encoding.ASCII) { NewLine = "\r\n", AutoFlush = true };
            StringBuilder all = new();
            await writer.WriteLineAsync("220 sahte ESMTP");
            bool inData = false;
            while (await reader.ReadLineAsync() is { } line)
            {
                all.AppendLine(line);
                if (inData)
                {
                    if (line == ".")
                    {
                        inData = false;
                        await writer.WriteLineAsync("250 tamam");
                    }

                    continue;
                }

                string command = line.Length >= 4 ? line[..4].ToUpperInvariant() : line.ToUpperInvariant();
                switch (command)
                {
                    case "EHLO":
                        await writer.WriteLineAsync("250-sahte");
                        await writer.WriteLineAsync("250 8BITMIME");
                        break;
                    case "DATA":
                        inData = true;
                        await writer.WriteLineAsync("354 devam");
                        break;
                    case "QUIT":
                        await writer.WriteLineAsync("221 hoşçakal");
                        return all.ToString();
                    default:
                        await writer.WriteLineAsync("250 tamam");
                        break;
                }
            }

            return all.ToString();
        }
    }
}
