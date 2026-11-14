namespace TahaKonsept.Agent.Engines.Image.Catalog;

/// <summary>Bir modelin indirilecek tek dosyası (Hugging Face deposu + depo içi yol + yerel yol + boyut + SHA-256).</summary>
/// <param name="Role">Dosyanın rolü (CLI bayrağını belirler).</param>
/// <param name="Repo">Hugging Face deposu (ör. <c>leejet/Z-Image-Turbo-GGUF</c>).</param>
/// <param name="RepoPath">Depo içindeki dosya yolu.</param>
/// <param name="LocalPath">Model klasörüne göre yerel yol (ör. <c>diffusion/z_image_turbo-Q3_K.gguf</c>); modeller arası paylaşılabilir.</param>
/// <param name="SizeBytes">Beklenen boyut (bayt).</param>
/// <param name="Sha256">Beklenen SHA-256 (küçük harf hex) ya da <c>null</c>.</param>
public sealed record ImageModelFile(ImageModelFileRole Role, string Repo, string RepoPath, string LocalPath, long SizeBytes, string? Sha256);
