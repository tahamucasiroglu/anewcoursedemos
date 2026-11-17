namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>
/// Başlatmadan önceki önizleme (spec 0020 Req 3): çalışacak aşamalar (<c>analysis · code · images · voice · render · merge</c>), online model
/// çağrısı yapılıp yapılmayacağı ve değişiklik olmadığı (yeni sürümde ayar, yeniden üretimde konsept metni). Tahmini tutar yoktur.
/// </summary>
/// <param name="Stages">Çalışacak aşamalar.</param>
/// <param name="Online">Online model çağrısı yapılacak.</param>
/// <param name="Unchanged">Değişiklik yok (başlat pasif).</param>
public sealed record VersionPreviewDto(IReadOnlyList<string> Stages, bool Online, bool Unchanged);
