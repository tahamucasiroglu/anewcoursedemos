using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Modules.AiGateway.Application.Dtos;

namespace Konsept.Api.Tests.Support;

/// <summary>
/// Konsept testlerinin ortak adımları (plan 0014): profil + Anthropic key + rol modelleri kurulumu, iş bitene kadar yoklama ve sahte
/// sağlayıcı yanıtlarının (konsept / öneri / denetim JSON'u) kurulması.
/// </summary>
public static class ConceptTestKit
{
    /// <summary>Konsept rolü modeli.</summary>
    public const string ConceptModel = "anthropic/claude-sonnet-5-5";

    /// <summary>Anlamca tekrar denetimi modeli.</summary>
    public const string DedupeModel = "anthropic/claude-haiku-5-5";

    /// <summary>Konu önerisi modeli.</summary>
    public const string TopicModel = "anthropic/claude-opus-5-5";

    /// <summary>Profil oluşturur, Anthropic key'i ekler ve üç rol için model seçer.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="ct">İptal.</param>
    /// <param name="name">Kanal adı.</param>
    /// <param name="seconds">Hedef süre.</param>
    /// <param name="wordCheck">Kelime denetimi.</param>
    /// <param name="language">Dil.</param>
    /// <returns>Profil.</returns>
    public static async Task<Guid> ReadyProfileAsync(
        HttpClient client, CancellationToken ct, string name = "Tarih Kanalı", int seconds = 60, bool wordCheck = false, string language = "tr")
    {
        ArgumentNullException.ThrowIfNull(client);
        using var created = await client.PostAsJsonAsync("/api/profiles", ProfileApiTests.Body(name, seconds: seconds, language: language, format: "portrait"), ct);
        created.EnsureSuccessStatusCode();
        var profile = (await created.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
        (await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = "sk-ant-test-" + Guid.NewGuid().ToString("N") }, ct)).EnsureSuccessStatusCode();
        await SelectModelsAsync(client, profile, ct, ConceptModel, DedupeModel, TopicModel, wordCheck);
        return profile;
    }

    /// <summary>Rol modellerini kaydeder (<c>null</c> rol boşaltılır).</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="profile">Profil.</param>
    /// <param name="ct">İptal.</param>
    /// <param name="concept">Konsept modeli.</param>
    /// <param name="dedupe">Denetim modeli.</param>
    /// <param name="topic">Öneri modeli.</param>
    /// <param name="wordCheck">Kelime denetimi.</param>
    /// <returns>Görev.</returns>
    public static async Task SelectModelsAsync(HttpClient client, Guid profile, CancellationToken ct, string? concept, string? dedupe, string? topic, bool wordCheck = false)
    {
        ArgumentNullException.ThrowIfNull(client);
        var body = new
        {
            models = new[]
            {
                new { role = "concept", modelId = concept, effort = (string?)null },
                new { role = "dedupe", modelId = dedupe, effort = (string?)null },
                new { role = "topic", modelId = topic, effort = (string?)null },
            },
            wordCheck,
        };
        (await client.PutAsJsonAsync($"/api/profiles/{profile}/concept-settings", body, ct)).EnsureSuccessStatusCode();
    }

    /// <summary>Üretim durumunu okur.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="profile">Profil.</param>
    /// <param name="ct">İptal.</param>
    /// <returns>Durum.</returns>
    public static async Task<JsonElement> WorkAsync(HttpClient client, Guid profile, CancellationToken ct) =>
        await client!.GetFromJsonAsync<JsonElement>($"/api/concepts/work?profileId={profile}", ct);

    /// <summary>Şeritteki iş bitene kadar bekler ve işi döndürür.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="profile">Profil.</param>
    /// <param name="lane">Şerit (<c>topicJob</c> · <c>ideaJob</c>).</param>
    /// <param name="ct">İptal.</param>
    /// <returns>İş.</returns>
    public static async Task<JsonElement> WaitJobAsync(HttpClient client, Guid profile, string lane, CancellationToken ct)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        while (true)
        {
            var work = await WorkAsync(client, profile, ct);
            var job = work.GetProperty(lane);
            if (job.ValueKind == JsonValueKind.Object && job.GetProperty("status").GetString() != "running")
            {
                return job;
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("İş bitmedi: " + work);
            }

            await Task.Delay(25, ct);
        }
    }

    /// <summary>Gönderip yanıt gövdesini döndürür.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="path">Yol.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="ct">İptal.</param>
    /// <returns>Yanıt.</returns>
    public static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, object body, CancellationToken ct) =>
        client!.PostAsJsonAsync(path, body, ct);

    /// <summary>Sahnelerden konsept JSON'u.</summary>
    /// <param name="title">Başlık.</param>
    /// <param name="durations">Sahne süreleri.</param>
    /// <param name="narration">Seslendirme (her sahnede).</param>
    /// <returns>Model çıktısı.</returns>
    public static string ConceptJson(string title, int[] durations, string? narration = null)
    {
        ArgumentNullException.ThrowIfNull(durations);
        var scenes = durations.Select((d, i) => new
        {
            title = $"Sahne {i + 1}",
            visual = "Eski bir harita yakınlaşır.",
            narration = narration ?? $"Bu bölümde {title} anlatılıyor, sahne {i + 1}.",
            durationSeconds = d,
        });
        return JsonSerializer.Serialize(new { title, scenes });
    }

    /// <summary>Konu önerisi JSON'u.</summary>
    /// <param name="topics">(başlık, özet) çiftleri.</param>
    /// <returns>Model çıktısı.</returns>
    public static string TopicsJson(params (string Title, string Summary)[] topics) =>
        JsonSerializer.Serialize(new { topics = topics.Select(t => new { title = t.Title, summary = t.Summary }) });

    /// <summary>Başarılı yanıt.</summary>
    /// <param name="text">Metin.</param>
    /// <returns>Deneme.</returns>
    public static Task<ProviderAttempt> Ok(string text) => Task.FromResult(ProviderAttempt.Completed(text, 100, 200));

    /// <summary>Denetim yanıtı: verilen aday numaraları tekrar.</summary>
    /// <param name="numbers">Numaralar.</param>
    /// <returns>Deneme.</returns>
    public static Task<ProviderAttempt> Duplicates(params int[] numbers) => Ok(JsonSerializer.Serialize(new { duplicates = numbers }));
}
