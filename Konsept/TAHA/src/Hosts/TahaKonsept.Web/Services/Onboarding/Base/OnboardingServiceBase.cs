using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Onboarding.Abstract;

namespace TahaKonsept.Web.Services.Onboarding.Base;

/// <summary>
/// <see cref="IOnboardingService"/> ortak davranışı: ham bilgileri (<see cref="CollectFactsAsync"/>, somut sınıfta API
/// çağrıları) adımlara çevirir (<see cref="Build"/>) ve sonucu kullanıcı başına <see cref="CacheDuration"/> kadar önbellekler
/// (hepsi tamamsa <see cref="CompleteCacheDuration"/> kadar — bitmiş liste her sayfada API'yi yormaz; eksik bilgi varsa
/// önbelleğe yazmaz). <see cref="Build"/> saf fonksiyondur; adım kuralları:
/// LLM anahtarı = metin modeli olan bir platformun anahtarı kayıtlı; masaüstü = en az bir eşlenmiş bilgisayar;
/// profil/konsept/video = en az bir kayıt.
/// </summary>
public abstract class OnboardingServiceBase : IOnboardingService
{
    /// <summary>Üst menü rozeti için önbellek süresi.</summary>
    public static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    /// <summary>Tüm adımlar tamamsa önbellek süresi (rozet zaten gizli; bitmiş liste nadiren geri döner).</summary>
    public static readonly TimeSpan CompleteCacheDuration = TimeSpan.FromHours(6);

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private readonly IMemoryCache _cache;

    /// <summary>Önbelleği alır.</summary>
    /// <param name="cache">Bellek önbelleği.</param>
    protected OnboardingServiceBase(IMemoryCache cache) => _cache = cache;

    /// <summary>Önbellek anahtarını kullanıcıya ayıran değer (ör. kullanıcı kimliği).</summary>
    protected abstract string CacheScope { get; }

    /// <inheritdoc />
    public async Task<OnboardingStatus> GetStatusAsync(bool refresh, CancellationToken ct = default)
    {
        var key = "onboarding-status:" + CacheScope;
        if (!refresh && _cache.TryGetValue(key, out OnboardingStatus? cached) && cached is not null)
        {
            return cached;
        }

        var status = Build(await CollectFactsAsync(ct));
        if (status.Steps.All(s => s.State != OnboardingStepState.Unknown))
        {
            _cache.Set(key, status, CacheDurationFor(status));
        }
        else
        {
            _cache.Remove(key);
        }

        return status;
    }

    /// <summary>Kontrol listesini ham bilgilerden kurar (<see cref="OnboardingStepKeys.All"/> sırasıyla).</summary>
    /// <param name="facts">Ham bilgiler.</param>
    public static OnboardingStatus Build(OnboardingFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        IReadOnlyList<OnboardingStep> steps =
        [
            LlmKeyStep(facts.LlmKeyPlatforms),
            AgentStep(facts.PairedAgents, facts.OnlineAgents),
            CountStep(OnboardingStepKeys.Profile, facts.Profiles, "profil"),
            CountStep(OnboardingStepKeys.Concept, facts.Concepts, "konsept"),
            VideoStep(facts.Videos),
        ];

        return new OnboardingStatus
        {
            Steps = steps,
            Remaining = steps.Count(s => s.State == OnboardingStepState.Waiting),
            IsComplete = steps.All(s => s.State == OnboardingStepState.Done),
        };
    }

    /// <summary>Durumun önbellekte kalma süresi: tamamsa <see cref="CompleteCacheDuration"/>, değilse <see cref="CacheDuration"/>.</summary>
    /// <param name="status">Kurulmuş durum.</param>
    public static TimeSpan CacheDurationFor(OnboardingStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        return status.IsComplete ? CompleteCacheDuration : CacheDuration;
    }

    /// <summary>Ham bilgileri toplar (alınamayan alan null kalır).</summary>
    /// <param name="ct">İptal belirteci.</param>
    protected abstract Task<OnboardingFacts> CollectFactsAsync(CancellationToken ct);

    private static OnboardingStep LlmKeyStep(IReadOnlyList<string>? platforms) => platforms switch
    {
        null => Unknown(OnboardingStepKeys.LlmKey),
        { Count: 0 } => Step(OnboardingStepKeys.LlmKey, OnboardingStepState.Waiting, "Henüz LLM anahtarı yok."),
        _ => Step(OnboardingStepKeys.LlmKey, OnboardingStepState.Done, string.Join(", ", platforms) + " kayıtlı."),
    };

    private static OnboardingStep AgentStep(int? paired, int? online)
    {
        if (paired is null)
        {
            return Unknown(OnboardingStepKeys.Agent);
        }

        if (paired <= 0)
        {
            return Step(OnboardingStepKeys.Agent, OnboardingStepState.Waiting, "Eşlenmiş bilgisayar yok.");
        }

        var detail = paired.Value.ToString(Turkish) + " bilgisayar eşli"
            + (online is > 0 ? " · " + online.Value.ToString(Turkish) + " çevrimiçi." : " · şu an bağlı değil.");
        return Step(OnboardingStepKeys.Agent, OnboardingStepState.Done, detail);
    }

    private static OnboardingStep CountStep(string key, int? count, string noun) => count switch
    {
        null => Unknown(key),
        <= 0 => Step(key, OnboardingStepState.Waiting, "Henüz " + noun + " yok."),
        _ => Step(key, OnboardingStepState.Done, count.Value.ToString(Turkish) + " " + noun + "."),
    };

    // Video listesi take=1 ile alınır (yalnız "en az bir" önemli); sayı gerçek toplam olmadığı için yazılmaz.
    private static OnboardingStep VideoStep(int? count) => count switch
    {
        null => Unknown(OnboardingStepKeys.Video),
        <= 0 => Step(OnboardingStepKeys.Video, OnboardingStepState.Waiting, "Henüz video yok."),
        _ => Step(OnboardingStepKeys.Video, OnboardingStepState.Done, "En az bir video üretildi."),
    };

    private static OnboardingStep Unknown(string key) =>
        Step(key, OnboardingStepState.Unknown, "Durum alınamadı (API yanıt vermedi).");

    private static OnboardingStep Step(string key, OnboardingStepState state, string detail) =>
        new() { Key = key, State = state, Detail = detail };
}
