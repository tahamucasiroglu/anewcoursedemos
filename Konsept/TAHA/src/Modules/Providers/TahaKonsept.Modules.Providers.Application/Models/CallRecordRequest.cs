using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// <c>ICallRecorder</c>'a verilen tek çağrı ölçümü: kim, hangi model, ne kadar kullanım, süre ve (başarısızsa) hata.
/// </summary>
/// <param name="Context">Kullanıcı + amaç.</param>
/// <param name="Model">Katalog kaydı.</param>
/// <param name="Usage">Kullanım.</param>
/// <param name="DurationMs">Süre (ms).</param>
/// <param name="Error">Hata (başarılıysa null).</param>
public sealed record CallRecordRequest(CallContext Context, ModelInfo Model, CallUsage Usage, long DurationMs, Error? Error);
