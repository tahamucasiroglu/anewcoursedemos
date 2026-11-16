using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>Durum, şerit ve iş kodlarının API'deki kararlı metinleri (Web ve resx bu kodları kullanır).</summary>
public static class ConceptCodes
{
    /// <summary>Varsayılan Üretilenler filtresi (S-5): Üretildi + Onaylandı.</summary>
    public static readonly IReadOnlyList<ConceptStatus> DefaultFilter = [ConceptStatus.Generated, ConceptStatus.Approved];

    /// <summary>Düzenlenebilir durumlar (spec 0015 Req 1, S-5): Üretildi + Onaylandı.</summary>
    public static readonly IReadOnlyList<ConceptStatus> Editable = [ConceptStatus.Generated, ConceptStatus.Approved];

    /// <summary>Revizyon durumu kodu.</summary>
    /// <param name="status">Durum.</param>
    /// <returns>Kod.</returns>
    public static string Of(RevisionStatus status) => status switch
    {
        RevisionStatus.Ready => "ready",
        RevisionStatus.Failed => "failed",
        _ => "running",
    };

    /// <summary>Durum kodu.</summary>
    /// <param name="status">Durum.</param>
    /// <returns>Kod.</returns>
    public static string Of(ConceptStatus status) => status switch
    {
        ConceptStatus.Approved => "approved",
        ConceptStatus.Queued => "queued",
        ConceptStatus.Producing => "producing",
        ConceptStatus.VideoReady => "video_ready",
        _ => "generated",
    };

    /// <summary>Durum kodunu çözer.</summary>
    /// <param name="code">Kod.</param>
    /// <param name="status">Durum.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    public static bool TryParse(string? code, out ConceptStatus status)
    {
        foreach (var candidate in Enum.GetValues<ConceptStatus>())
        {
            if (string.Equals(Of(candidate), code, StringComparison.Ordinal))
            {
                status = candidate;
                return true;
            }
        }

        status = default;
        return false;
    }

    /// <summary>Şerit kodu.</summary>
    /// <param name="lane">Şerit.</param>
    /// <returns>Kod.</returns>
    public static string Of(JobLane lane) => lane == JobLane.Idea ? "idea" : "topic";

    /// <summary>İş türü kodu.</summary>
    /// <param name="kind">Tür.</param>
    /// <returns>Kod.</returns>
    public static string Of(JobKind kind) => kind switch
    {
        JobKind.FromTopics => "from_topics",
        JobKind.FromIdea => "from_idea",
        _ => "topics",
    };

    /// <summary>İş durumu kodu.</summary>
    /// <param name="status">Durum.</param>
    /// <returns>Kod.</returns>
    public static string Of(JobStatus status) => status switch
    {
        JobStatus.Completed => "completed",
        JobStatus.Stopped => "stopped",
        JobStatus.Failed => "failed",
        _ => "running",
    };
}
