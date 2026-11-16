namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Üretim sırasındaki satır (spec 0006 Req 11, 13).</summary>
/// <param name="Id">İş.</param>
/// <param name="ConceptId">Konsept.</param>
/// <param name="Title">Konsept başlığı.</param>
/// <param name="Number">Sıra numarası (1'den).</param>
/// <param name="Status">Durum: <c>queued</c> · <c>producing</c> · <c>stopped</c> · <c>waiting_for_agent</c> · <c>failed</c> · <c>completed</c>.</param>
/// <param name="Language">Konseptin kaydındaki dil.</param>
/// <param name="Fixed">Sabitlenen ayarlar ve konsept değerleri; iş hiç başlamadıysa <c>null</c>.</param>
public sealed record ProductionJobDto(Guid Id, Guid ConceptId, string Title, int Number, string Status, string Language, FixedSettingsDto? Fixed);
