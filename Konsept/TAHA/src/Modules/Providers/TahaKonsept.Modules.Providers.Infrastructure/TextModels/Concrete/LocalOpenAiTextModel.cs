using System.Net.Http.Headers;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;

/// <summary>
/// Yerel LLM (OpenAI uyumlu: Ollama / LM Studio / llama.cpp server). Kök adres kimlik bilgisinden (<see cref="ProviderCredential.BaseUrl"/>,
/// ör. <c>http://localhost:11434/v1</c>), model kimliği geçiş kaydından (<c>local-openai:&lt;model&gt;</c>). Anahtar boşsa yetki başlığı
/// gönderilmez; katalogda efor seviyesi olmadığı için <c>reasoning_effort</c> gönderilmez; web araması desteklenmez. Katalog fiyatı 0.
/// </summary>
public sealed class LocalOpenAiTextModel : OpenAiCompatibleTextModelBase
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı (geçiş kimliği çözülmüş).</param>
    /// <param name="credential">Kimlik bilgisi (kök adres dolu).</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public LocalOpenAiTextModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies, http)
    {
        ArgumentNullException.ThrowIfNull(credential);
        if (string.IsNullOrWhiteSpace(credential.BaseUrl) || !Uri.TryCreate(credential.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Yerel LLM için geçerli bir kök adres gerekli.", nameof(credential));
        }

        BaseUri = uri;
    }

    /// <inheritdoc />
    protected override Uri BaseUri { get; }

    /// <inheritdoc />
    protected override void ApplyAuthorization(HttpRequestMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!string.IsNullOrWhiteSpace(Credential.ApiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Credential.ApiKey);
        }
    }
}
