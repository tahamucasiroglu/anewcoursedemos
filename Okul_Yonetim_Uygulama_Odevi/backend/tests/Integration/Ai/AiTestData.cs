using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Ai;
using OkulYonetim.Tests.Integration.Account;
using OkulYonetim.Tests.Integration.Communication;
using OkulYonetim.Tests.Integration.Infrastructure;
using static OkulYonetim.Tests.Integration.Administration.AdminTestData;
using static OkulYonetim.Tests.Integration.Attendance.AttendanceTestData;
using static OkulYonetim.Tests.Integration.Identity.IdentityTestData;

namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Spec 0012 test yardımcıları: sahte ses ve metin servisli (verilmezse ayarlardaki — Testing ortamında adresler boş:
/// "kullanılamıyor"), bildirimi ve e-postayı yakalayan, sabit saatli API; sesli not (multipart), "Vazgeç", taslakla gönderim
/// uçları; veritabanındaki taslaklar ve konuşmanın mesaj sayısı.
/// </summary>
public static class AiTestData
{
    public static OkulApiFactory Factory(TestDatabase database, ISpeechToText? speech, IAiTextGenerator? text, CapturingPushNotifier? notifier = null, Action<IServiceCollection>? configure = null) =>
        new(database.ConnectionString, timeProvider: new FakeTimeProvider(Morning), configureServices: services =>
        {
            services.RemoveAll<IEmailOutbox>();
            services.AddSingleton<IEmailOutbox>(new CapturingOutbox());
            services.RemoveAll<IPushNotifier>();
            services.AddSingleton<IPushNotifier>(notifier ?? new CapturingPushNotifier());
            if (speech is not null)
            {
                services.RemoveAll<ISpeechToText>();
                services.AddSingleton(speech);
            }

            if (text is not null)
            {
                services.RemoveAll<IAiTextGenerator>();
                services.AddSingleton(text);
            }

            configure?.Invoke(services);
        });

    public static byte[] Audio(int length = 64 * 1024)
    {
        byte[] audio = new byte[length];
        for (int i = 0; i < audio.Length; i++)
        {
            audio[i] = (byte)(i % 251);
        }

        return audio;
    }

    public static async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> VoiceNoteAsync(
        HttpClient client, string? token, Guid conversationId, byte[]? audio = null, string contentType = "audio/m4a", bool withFile = true)
    {
        using MultipartFormDataContent form = new();
        form.Add(new StringContent(conversationId.ToString()), "conversationId");
        if (withFile)
        {
            ByteArrayContent file = new(audio ?? Audio());
            file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            form.Add(file, "audio", "not.m4a");
        }

        using HttpRequestMessage request = new(HttpMethod.Post, new Uri("/api/ai/voice-message-drafts", UriKind.Relative)) { Content = form };
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        string raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonElement json = string.IsNullOrEmpty(raw) || !raw.StartsWith('{') ? default : JsonDocument.Parse(raw).RootElement.Clone();
        return (response.StatusCode, json, raw);
    }

    public static async Task<Guid> DraftedAsync(HttpClient client, string token, Guid conversationId)
    {
        var (status, json, raw) = await VoiceNoteAsync(client, token, conversationId);
        if (status != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"taslak oluşmadı: {status} {raw}");
        }

        return json.GetProperty("draftId").GetGuid();
    }

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> SendDraftAsync(HttpClient client, string token, Guid conversationId, string body, Guid aiDraftId) =>
        CallAsync(client, HttpMethod.Post, $"/api/conversations/{conversationId}/messages", token, new { body, aiDraftId });

    public static Task<(HttpStatusCode Status, JsonElement Body, string Raw)> DiscardAsync(HttpClient client, string token, Guid draftId) =>
        CallAsync(client, HttpMethod.Delete, $"/api/ai/drafts/{draftId}", token);

    public static Task<List<AiDraft>> DraftsAsync(OkulApiFactory factory, Guid tenantId) =>
        QueryAsync(factory, c => c.AiDrafts.IgnoreQueryFilters().AsNoTracking().Where(d => d.TenantId == tenantId).ToListAsync());

    public static Task<int> MessageCountAsync(OkulApiFactory factory, Guid conversationId) =>
        QueryAsync(factory, c => c.ConversationMessages.IgnoreQueryFilters().CountAsync(m => m.ConversationId == conversationId));
}
