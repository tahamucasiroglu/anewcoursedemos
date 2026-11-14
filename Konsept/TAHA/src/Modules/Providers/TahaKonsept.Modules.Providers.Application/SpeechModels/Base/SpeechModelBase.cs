using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.AiModels.Base;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.SpeechModels.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.SpeechModels.Base;

/// <summary>
/// Online ses modellerinin ortak tabanı. Somut sınıf yalnızca <see cref="SynthesizeCoreAsync"/>'i yazar (tek HTTP çağrısı);
/// doğrulama, yeniden deneme ve CallLog/maliyet burada. Somut sınıflar <c>Infrastructure/SpeechModels/Concrete</c> altına eklenir.
/// </summary>
public abstract class SpeechModelBase : AiModelBase, ISpeechModel
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    protected SpeechModelBase(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies)
        : base(model, credential, dependencies)
    {
    }

    /// <inheritdoc />
    public async Task<Result<SpeechResult>> SynthesizeAsync(SpeechRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Error.Validation(ProviderErrorCodes.InvalidRequest, "Seslendirilecek metin boş.");
        }

        var call = await ExecuteMeteredAsync(request.Context, c => SynthesizeCoreAsync(request, c), o => o.Usage, ct);
        return call.Map(m => new SpeechResult(m.Value.Audio, m.Value.MimeType, m.Value.AudioSeconds, Model.Key, m.CostUsd, m.DurationMs));
    }

    /// <summary>Tek HTTP çağrısı ile seslendirir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected abstract Task<Result<SpeechModelOutput>> SynthesizeCoreAsync(SpeechRequest request, CancellationToken ct);
}
