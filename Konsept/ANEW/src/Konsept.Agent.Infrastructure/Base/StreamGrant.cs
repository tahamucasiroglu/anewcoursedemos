namespace Konsept.Agent.Infrastructure.Base;

/// <summary>Bir yerel akış yetkisi (spec 0017 Req 21): video, dosya, ilk açılışın son anı ve son kullanım.</summary>
/// <param name="VideoId">Video.</param>
/// <param name="Path">Dosya yolu.</param>
/// <param name="OpenUntil">İlk açılışın son anı (UTC).</param>
/// <param name="LastUse">Son kullanım (UTC); hiç açılmadıysa <c>null</c>.</param>
public sealed record StreamGrant(Guid VideoId, string Path, DateTimeOffset OpenUntil, DateTimeOffset? LastUse = null);
