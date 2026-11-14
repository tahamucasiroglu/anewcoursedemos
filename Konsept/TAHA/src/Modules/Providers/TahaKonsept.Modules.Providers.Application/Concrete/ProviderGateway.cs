using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.TextModels.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Concrete;

/// <summary>
/// <see cref="IProviderGateway"/> uygulaması: işi doğrular, katalogdan modeli bulur, anahtarı çözer, model sınırlarına göre
/// <see cref="TextRequest"/> kurar (çıkış sınırı, efor, web araması, 16000 token üstünde akış) ve somut modeli çalıştırır.
/// </summary>
public sealed class ProviderGateway : ProvidersServiceBase, IProviderGateway
{
    /// <summary>Çıkış sınırı verilmezse kullanılan üst değer.</summary>
    public const int DefaultMaxOutputTokens = 16000;

    /// <summary>Bu değerin üstündeki çıkış sınırlarında akış kullanılır.</summary>
    public const int StreamingThreshold = 16000;

    private readonly IModelCatalog _catalog;
    private readonly IApiKeyVault _vault;
    private readonly ITextModelFactory _factory;
    private readonly ILogger<ProviderGateway> _logger;

    /// <summary>Kurucu.</summary>
    /// <param name="catalog">Katalog.</param>
    /// <param name="vault">Anahtar kasası.</param>
    /// <param name="factory">Model fabrikası.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Logger.</param>
    public ProviderGateway(IModelCatalog catalog, IApiKeyVault vault, ITextModelFactory factory, IClock clock, ILogger<ProviderGateway> logger)
        : base(clock)
    {
        _catalog = catalog;
        _vault = vault;
        _factory = factory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<TextResult<T>>> RunStructuredAsync<T>(TextTask task, CancellationToken ct)
    {
        var prepared = await PrepareAsync(task, ct);
        return prepared.IsFailure ? prepared.Error : await prepared.Value.Model.RunAsync<T>(prepared.Value.Request, ct);
    }

    /// <inheritdoc />
    public async Task<Result<TextResult<string>>> RunTextAsync(TextTask task, CancellationToken ct)
    {
        var prepared = await PrepareAsync(task, ct);
        return prepared.IsFailure ? prepared.Error : await prepared.Value.Model.RunTextAsync(prepared.Value.Request, ct);
    }

    /// <summary>İşi doğrular ve model + isteği hazırlar.</summary>
    /// <param name="task">İş.</param>
    /// <param name="ct">İptal belirteci.</param>
    private async Task<Result<(ITextModel Model, TextRequest Request)>> PrepareAsync(TextTask task, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.UserId == Guid.Empty)
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, "Kullanıcı kimliği gerekli.");
        }

        if (string.IsNullOrWhiteSpace(task.UserPrompt))
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, "Kullanıcı promptu boş olamaz.");
        }

        var model = _catalog.Find(task.ModelKey);
        if (model is null)
        {
            return Error.NotFound(ProviderErrorCodes.ModelNotFound, $"Model katalogda yok: {task.ModelKey}");
        }

        if (model.Capability != ModelCapability.Text || model.RunsOn != ModelRunsOn.Server)
        {
            return Error.Validation(ProviderErrorCodes.ModelNotSupported, $"{model.DisplayName} bir online metin modeli değil.");
        }

        var credential = await _vault.GetCredentialAsync(task.UserId, model.Platform, ct);
        if (credential.IsFailure)
        {
            return credential.Error;
        }

        var created = _factory.Create(model, credential.Value);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var maxOutput = ResolveMaxOutput(task.MaxOutputTokens, model.MaxOutputTokens);
        var useWebSearch = task.UseWebSearch && model.SupportsWebSearch;
        if (task.UseWebSearch && !useWebSearch)
        {
            _logger.LogWarning("{Model} web aramasını desteklemiyor; istek web araması olmadan gönderiliyor ({Purpose}).", model.Key, task.Purpose);
        }

        var request = new TextRequest
        {
            Context = new CallContext(task.UserId, string.IsNullOrWhiteSpace(task.Purpose) ? "unspecified" : task.Purpose.Trim()),
            SystemPrompt = task.SystemPrompt ?? string.Empty,
            UserPrompt = task.UserPrompt,
            MaxOutputTokens = maxOutput,
            Effort = task.Effort,
            UseWebSearch = useWebSearch,
            CacheSystemPrompt = task.CacheSystemPrompt,
            Stream = maxOutput > StreamingThreshold,
        };
        return (created.Value, request);
    }

    /// <summary>Çıkış sınırını model sınırına göre belirler.</summary>
    /// <param name="requested">İstenen (0 = varsayılan).</param>
    /// <param name="modelLimit">Model sınırı.</param>
    private static int ResolveMaxOutput(int requested, int? modelLimit)
    {
        var limit = modelLimit is > 0 ? modelLimit.Value : int.MaxValue;
        var wanted = requested > 0 ? requested : DefaultMaxOutputTokens;
        return Math.Min(wanted, limit);
    }
}
