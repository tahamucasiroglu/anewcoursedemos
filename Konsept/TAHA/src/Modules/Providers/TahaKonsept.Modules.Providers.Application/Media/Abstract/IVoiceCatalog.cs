using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Media.Abstract;

/// <summary>
/// Online ses modellerinin seçilebilir sesleri (statik liste; Türkçeye özgü sesler önce). <c>GET api/providers/voices</c> ve
/// <c>IMediaGateway</c> (istek ses vermezse varsayılan ses) kullanır. Önemli üyeler: <see cref="List"/>, <see cref="DefaultVoiceId"/>.
/// </summary>
public interface IVoiceCatalog
{
    /// <summary>Modelin seslerini listeler.</summary>
    /// <param name="modelKey">Katalog anahtarı (online ses modeli).</param>
    /// <returns>Sesler ya da <c>providers.invalid_request</c> / <c>model_not_found</c> / <c>model_not_supported</c>.</returns>
    Result<IReadOnlyList<VoiceInfo>> List(string? modelKey);

    /// <summary>Modelin varsayılan ses kimliği.</summary>
    /// <param name="modelKey">Katalog anahtarı.</param>
    /// <returns>Ses kimliği ya da modelin listesi yoksa null.</returns>
    string? DefaultVoiceId(string modelKey);
}
