using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajanın kurulu kopyası (spec 0023 Req 9–11; B-77): <c>%LOCALAPPDATA%\anewconcept\app</c> altındaki exe, tek önceki sürüm, güncelleme durumu,
/// HKCU başlangıç + "Uygulamalar" kaydı, Başlat menüsü kısayolu ve kaldırma.
/// </summary>
public interface IAgentInstallation
{
    /// <summary>Gets kurulum yeri.</summary>
    string InstallRoot { get; }

    /// <summary>Gets kurulu exe.</summary>
    string InstalledExe { get; }

    /// <summary>Gets güncellemenin indirildiği geçici klasör.</summary>
    string StagingDirectory { get; }

    /// <summary>Güncelleme durumunu okur (yoksa boş).</summary>
    /// <returns>Durum.</returns>
    UpdateState LoadState();

    /// <summary>Güncelleme durumunu yazar.</summary>
    /// <param name="state">Durum.</param>
    void SaveState(UpdateState state);

    /// <summary>
    /// Doğrulanmış yeni exe'yi kurar: daha eski önceki sürüm silinir, çalışan sürüm "önceki" olur, yeni exe kurulu yere geçer (Req 9: yalnızca bir
    /// önceki sürüm).
    /// </summary>
    /// <param name="stagedExe">Doğrulanmış exe.</param>
    void Swap(string stagedExe);

    /// <summary>Önceki sürümü kurulu yere geri koyar (başarısız yeni sürüm silinir).</summary>
    /// <returns>Önceki sürüm varsa ve geri konduysa <c>true</c>.</returns>
    bool RollBack();

    /// <summary>Kurulu exe'yi başlatır.</summary>
    /// <param name="arguments">Argümanlar.</param>
    void LaunchInstalled(string arguments);

    /// <summary>
    /// İlk açılış (B-77): çalışan exe kurulu yerde değilse oraya kopyalanır; her durumda HKCU başlangıç kaydı, "Uygulamalar" kaydı
    /// ("ANEW Konsept") ve Başlat menüsü kısayolu yazılır.
    /// </summary>
    /// <param name="runningExe">Çalışan exe.</param>
    /// <param name="version">Sürüm.</param>
    /// <returns>Kopyalandıysa (kurulu kopya başlatılmalı, bu süreç kapanmalı) <c>true</c>.</returns>
    bool EnsureInstalled(string runningExe, string version);

    /// <summary>
    /// Kaldırma (Req 11): çalışan ajan kapatılır; kurulum yeri, kısayol, başlangıç ve kaldırma kaydı silinir. Videolar yalnızca istenirse silinir;
    /// başka hiçbir klasöre ya da ortam ayarına dokunulmaz.
    /// </summary>
    /// <param name="deleteVideos">"Videolarınız da silinsin mi?" cevabı.</param>
    /// <param name="videoFolder">Video klasörü.</param>
    /// <returns>Her şey silindiyse <c>true</c>.</returns>
    bool Uninstall(bool deleteVideos, string? videoFolder);
}
