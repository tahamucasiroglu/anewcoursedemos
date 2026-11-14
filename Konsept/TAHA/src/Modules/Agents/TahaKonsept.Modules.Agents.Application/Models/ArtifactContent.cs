namespace TahaKonsept.Modules.Agents.Application.Models;

/// <summary>
/// İndirilmek üzere açılmış artefakt: içerik akışı (çağıran kapatır / HTTP yanıtı kapatır), içerik türü, dosya adı ve boyut.
/// </summary>
/// <param name="Content">Okunabilir akış.</param>
/// <param name="ContentType">İçerik türü.</param>
/// <param name="FileName">İndirme dosya adı.</param>
/// <param name="Length">Boyut (bayt).</param>
public sealed record ArtifactContent(Stream Content, string ContentType, string FileName, long Length);
