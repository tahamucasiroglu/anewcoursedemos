namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Admin hata bildirimi listesi isteği (sorgu metni; spec 0025 Req 1, 3).</summary>
/// <param name="Page">Sayfa (1'den; geçersizse 1).</param>
/// <param name="Source">Kaynak: <c>all</c> (varsayılan) / <c>web</c> / <c>agent</c> / <c>api</c>.</param>
/// <param name="Status">Durum: <c>open</c> (varsayılan) / <c>new</c> / <c>reviewing</c> / <c>resolved</c> / <c>ignored</c> / <c>all</c>.</param>
public sealed record AdminErrorReportsRequest(int Page = 1, string? Source = null, string? Status = null);
