namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Admin kullanıcı listesi isteği (spec 0024 Req 1–2).</summary>
/// <param name="Page">Sayfa (1'den).</param>
/// <param name="Search">Ad/e-posta araması (en az 2 karakter).</param>
/// <param name="Status">Durum: <c>all</c> | <c>active</c> | <c>banned</c>.</param>
public sealed record AdminUsersRequest(int? Page, string? Search, string? Status);
