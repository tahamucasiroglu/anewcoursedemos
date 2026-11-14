namespace TahaKonsept.Agent.Core.Downloads;

/// <summary>İndirilen dosya: yol, boyut ve SHA256 (hex, küçük harf).</summary>
/// <param name="Path">Dosya yolu.</param>
/// <param name="SizeBytes">Boyut.</param>
/// <param name="Sha256">SHA256.</param>
public sealed record DownloadedFile(string Path, long SizeBytes, string Sha256);
