using System.Text.Json;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Bekleyen bildirim deposunun ortak mantığı (spec 0022 Req 7): JSON biçimi; okunamayan ya da bozuk içerik boş durum sayılır (ajan
/// açılışı engellenmez). Saklama yeri alt sınıftadır.
/// </summary>
public abstract class ErrorReportQueueBase : IErrorReportQueue
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public PendingReportState Load()
    {
        try
        {
            var text = Read();
            return string.IsNullOrWhiteSpace(text)
                ? PendingReportState.Empty
                : JsonSerializer.Deserialize<PendingReportState>(text, Json) ?? PendingReportState.Empty;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return PendingReportState.Empty;
        }
    }

    /// <inheritdoc />
    public void Save(PendingReportState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Write(JsonSerializer.Serialize(state, Json));
    }

    /// <summary>Saklı metni okur.</summary>
    /// <returns>Metin ya da <c>null</c>.</returns>
    protected abstract string? Read();

    /// <summary>Metni saklar.</summary>
    /// <param name="text">Metin.</param>
    protected abstract void Write(string text);
}
