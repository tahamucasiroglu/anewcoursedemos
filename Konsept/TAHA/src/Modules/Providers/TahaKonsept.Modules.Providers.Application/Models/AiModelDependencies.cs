using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Tüm model sınıflarının (metin/görsel/ses) ortak bağımlılıkları: çağrı kaydedici (CallLog + maliyet) ve logger.
/// Fabrika tarafından oluşturulup model kurucusuna verilir.
/// </summary>
/// <param name="Recorder">Çağrı kaydedici.</param>
/// <param name="Logger">Logger.</param>
public sealed record AiModelDependencies(ICallRecorder Recorder, ILogger Logger);
