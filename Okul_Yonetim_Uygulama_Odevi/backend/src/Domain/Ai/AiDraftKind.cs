namespace OkulYonetim.Domain.Ai;

/// <summary>AI taslağının türü: öğretmenin sesli notundan veli mesajı (spec 0012) ve öğrenci-hafta başına haftalık gelişim özeti (spec 0014).</summary>
public enum AiDraftKind
{
    VoiceMessage = 1,
    WeeklySummary = 2,
}
