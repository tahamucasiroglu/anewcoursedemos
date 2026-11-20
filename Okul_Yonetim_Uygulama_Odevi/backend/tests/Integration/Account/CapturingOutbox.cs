using System.Collections.Concurrent;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Tests.Integration.Account;

/// <summary>Kuyruğa bırakılan e-postaları saklar (gönderim yerine).</summary>
public sealed class CapturingOutbox : IEmailOutbox
{
    public ConcurrentQueue<EmailMessage> Messages { get; } = new();

    public void Enqueue(EmailMessage message) => this.Messages.Enqueue(message);

    public EmailMessage[] To(string address) => [.. this.Messages.Where(m => string.Equals(m.ToAddress, address, StringComparison.OrdinalIgnoreCase))];
}
