using System.Globalization;
using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Production.Domain.Constants;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Domain.Entities;

/// <summary>
/// Okunuş sözlüğü kaydı (kullanıcı düzeyi): yazılış → okunuş (Google → Gugıl). Konseptten gelen öneriler
/// <see cref="PronunciationStatus.Suggested"/> ile düşer, kullanıcı onaylayınca <see cref="PronunciationStatus.Approved"/> olur.
/// <see cref="WrittenKey"/> = yazılışın tr-TR küçük harfli hali (tekillik ve arama için).
/// </summary>
public sealed class Pronunciation : AggregateRoot, IHasUser, IAuditable
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>EF Core için boş kurucu.</summary>
    private Pronunciation()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Yazılış.</summary>
    public string Written { get; private set; } = string.Empty;

    /// <summary>Yazılış anahtarı (tr-TR küçük harf).</summary>
    public string WrittenKey { get; private set; } = string.Empty;

    /// <summary>Okunuş.</summary>
    public string Spoken { get; private set; } = string.Empty;

    /// <summary>Durum.</summary>
    public PronunciationStatus Status { get; private set; }

    /// <summary>Öneriyi getiren konsept.</summary>
    public Guid? ConceptId { get; private set; }

    /// <summary>Kaynak açıklaması.</summary>
    public string? Source { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Yeni kayıt oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="written">Yazılış.</param>
    /// <param name="spoken">Okunuş.</param>
    /// <param name="status">Durum.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="source">Kaynak.</param>
    /// <returns>Kayıt.</returns>
    public static Pronunciation Create(Guid userId, string written, string spoken, PronunciationStatus status, Guid? conceptId = null, string? source = null)
    {
        var entry = new Pronunciation { UserId = userId, ConceptId = conceptId, Source = ProductionLimits.ClipOrNull(source, ProductionLimits.TitleMaxLength) };
        entry.Update(written, spoken, status);
        return entry;
    }

    /// <summary>Yazılış anahtarı üretir.</summary>
    /// <param name="written">Yazılış.</param>
    /// <returns>Anahtar.</returns>
    public static string KeyOf(string written) =>
        ProductionLimits.Clip(written, ProductionLimits.PronunciationMaxLength).ToLower(Turkish);

    /// <summary>Alanları günceller.</summary>
    /// <param name="written">Yazılış.</param>
    /// <param name="spoken">Okunuş.</param>
    /// <param name="status">Durum.</param>
    public void Update(string written, string spoken, PronunciationStatus status)
    {
        Written = ProductionLimits.Clip(written, ProductionLimits.PronunciationMaxLength);
        WrittenKey = KeyOf(written);
        Spoken = ProductionLimits.Clip(spoken, ProductionLimits.PronunciationMaxLength);
        Status = status;
    }
}
