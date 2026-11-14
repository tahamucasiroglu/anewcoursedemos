namespace TahaKonsept.Agent.Core.Prerequisites;

/// <summary>İndirilecek ön gereksinim paketi.</summary>
/// <param name="Url">Adres.</param>
/// <param name="FileName">Yerel dosya adı.</param>
/// <param name="Sha256">Beklenen SHA256 (bilinmiyorsa <c>null</c>).</param>
/// <param name="Version">Sürüm (kurulum işaretine yazılır).</param>
public sealed record PrerequisiteDownload(Uri Url, string FileName, string? Sha256, string Version);
