using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Konsept aggregate'i: sahip kullanıcı, profil, durum (<see cref="ConceptStatus"/>), kaynak (<see cref="ConceptOrigin"/>),
/// belge (<see cref="Document"/>, veritabanında jsonb), başlık, token/maliyet toplamları, onay/silinme anları ve düzeltme bağı.
/// Durum geçişleri: Generated → Approved (<see cref="Approve"/>); Generated/Approved → Deleted (<see cref="MarkDeleted"/>);
/// Deleted → Generated (<see cref="Restore"/>). Silinmiş konsept düzenlenemez/onaylanamaz; kalıcı silme yalnız silinmişte (<see cref="CanPurge"/>).
/// </summary>
public sealed class Concept : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private Concept()
    {
    }

    private Concept(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Üretildiği profil.</summary>
    public Guid? ProfileId { get; private set; }

    /// <summary>Durum.</summary>
    public ConceptStatus Status { get; private set; }

    /// <summary>Kaynak.</summary>
    public ConceptOrigin Origin { get; private set; }

    /// <summary>Başlık (belgedeki başlıkla eşit tutulur).</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Konsept belgesi.</summary>
    public ConceptDocument Document { get; private set; } = new();

    /// <summary>Üreten modelin katalog anahtarı.</summary>
    public string ModelKey { get; private set; } = string.Empty;

    /// <summary>Toplam giriş token'ı.</summary>
    public long InputTokens { get; private set; }

    /// <summary>Toplam çıkış token'ı.</summary>
    public long OutputTokens { get; private set; }

    /// <summary>Toplam maliyet ($).</summary>
    public decimal CostUsd { get; private set; }

    /// <summary>Türetildiği konsept.</summary>
    public Guid? RevisionOfId { get; private set; }

    /// <summary>Onaylanma anı (UTC).</summary>
    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    /// <summary>Silinme anı (UTC).</summary>
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    /// <summary>Oluşturulma anı (UTC; kayıtta otomatik).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Son güncellenme anı (UTC; kayıtta otomatik).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Kalıcı silinebilir mi (yalnız silinmiş konseptler).</summary>
    public bool CanPurge => Status == ConceptStatus.Deleted;

    /// <summary>Yeni (Generated) konsept oluşturur.</summary>
    /// <param name="userId">Sahip.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="origin">Kaynak.</param>
    /// <param name="document">Belge.</param>
    /// <param name="modelKey">Model anahtarı.</param>
    /// <param name="inputTokens">Giriş token'ı.</param>
    /// <param name="outputTokens">Çıkış token'ı.</param>
    /// <param name="costUsd">Maliyet.</param>
    /// <param name="revisionOfId">Türetildiği konsept.</param>
    /// <returns>Konsept.</returns>
    public static Concept Create(
        Guid userId,
        Guid? profileId,
        ConceptOrigin origin,
        ConceptDocument document,
        string modelKey,
        long inputTokens,
        long outputTokens,
        decimal costUsd,
        Guid? revisionOfId = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var concept = new Concept(userId)
        {
            ProfileId = profileId,
            Origin = origin,
            Status = ConceptStatus.Generated,
            ModelKey = Truncate(modelKey ?? string.Empty, ConceptLimits.ModelKeyMaxLength),
            RevisionOfId = revisionOfId,
        };
        concept.SetDocument(document, null);
        concept.AddUsage(inputTokens, outputTokens, costUsd);
        return concept;
    }

    /// <summary>Belgeyi doğrudan düzenler (silinmişte hata).</summary>
    /// <param name="document">Yeni belge.</param>
    /// <param name="title">Başlık (boşsa belgedeki).</param>
    /// <returns>Sonuç.</returns>
    public Result UpdateDocument(ConceptDocument document, string? title)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (Status == ConceptStatus.Deleted)
        {
            return Result.Failure(ConceptErrors.IsDeleted);
        }

        if (document.Scenes.Count == 0)
        {
            return Result.Failure(ConceptErrors.EmptyDocument);
        }

        SetDocument(document, title);
        return Result.Success();
    }

    /// <summary>Düzeltme adayını kabul eder: belge değişir, maliyet eklenir.</summary>
    /// <param name="document">Aday belge.</param>
    /// <param name="inputTokens">Giriş token'ı.</param>
    /// <param name="outputTokens">Çıkış token'ı.</param>
    /// <param name="costUsd">Maliyet.</param>
    /// <returns>Sonuç.</returns>
    public Result AcceptRevision(ConceptDocument document, long inputTokens, long outputTokens, decimal costUsd)
    {
        var result = UpdateDocument(document, null);
        if (result.IsSuccess)
        {
            AddUsage(inputTokens, outputTokens, costUsd);
        }

        return result;
    }

    /// <summary>Kullanım ve maliyet ekler (ör. kelime denetimi, atılan düzeltmeler).</summary>
    /// <param name="inputTokens">Giriş token'ı.</param>
    /// <param name="outputTokens">Çıkış token'ı.</param>
    /// <param name="costUsd">Maliyet.</param>
    public void AddUsage(long inputTokens, long outputTokens, decimal costUsd)
    {
        InputTokens += Math.Max(0, inputTokens);
        OutputTokens += Math.Max(0, outputTokens);
        CostUsd += Math.Max(0, costUsd);
    }

    /// <summary>Onaylar (Generated → Approved; zaten onaylıysa değişmez).</summary>
    /// <param name="nowUtc">Şimdi.</param>
    /// <returns>Sonuç.</returns>
    public Result Approve(DateTimeOffset nowUtc)
    {
        if (Status == ConceptStatus.Deleted)
        {
            return Result.Failure(ConceptErrors.IsDeleted);
        }

        if (Status != ConceptStatus.Approved)
        {
            Status = ConceptStatus.Approved;
            ApprovedAtUtc = nowUtc;
        }

        return Result.Success();
    }

    /// <summary>Silinenler'e taşır (zaten silinmişse değişmez).</summary>
    /// <param name="nowUtc">Şimdi.</param>
    public void MarkDeleted(DateTimeOffset nowUtc)
    {
        if (Status == ConceptStatus.Deleted)
        {
            return;
        }

        Status = ConceptStatus.Deleted;
        DeletedAtUtc = nowUtc;
    }

    /// <summary>Silinenler'den geri alır (Deleted → Generated; onay sıfırlanır).</summary>
    /// <returns>Sonuç.</returns>
    public Result Restore()
    {
        if (Status != ConceptStatus.Deleted)
        {
            return Result.Failure(ConceptErrors.NotDeleted);
        }

        Status = ConceptStatus.Generated;
        DeletedAtUtc = null;
        ApprovedAtUtc = null;
        return Result.Success();
    }

    private void SetDocument(ConceptDocument document, string? title)
    {
        var finalTitle = Truncate((string.IsNullOrWhiteSpace(title) ? document.Title : title).Trim(), ConceptLimits.TitleMaxLength);
        Document = document with
        {
            Title = finalTitle,
            Scenes = document.Scenes.Select((s, i) => s with { Index = i }).ToList(),
        };
        Title = finalTitle;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
