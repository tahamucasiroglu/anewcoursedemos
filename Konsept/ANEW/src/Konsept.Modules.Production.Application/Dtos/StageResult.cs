using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Bir aşamanın sonucu (hat içi): devam · hata (sade açıklama) · durduruldu · ajan bekleniyor; yerel aşamada aygıt.</summary>
/// <param name="Outcome">Sonuç (<see cref="PipelineOutcome.Continue"/> = aşama tamamlandı).</param>
/// <param name="Error">Sade hata açıklaması.</param>
/// <param name="Device">Aygıt.</param>
public sealed record StageResult(PipelineOutcome Outcome, string? Error = null, string? Device = null);
