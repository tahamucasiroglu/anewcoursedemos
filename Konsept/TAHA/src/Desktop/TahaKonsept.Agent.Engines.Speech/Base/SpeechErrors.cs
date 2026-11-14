using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Speech.Base;

/// <summary>Ses motorlarının döndürdüğü hata kodları ve Türkçe mesajları (tek yerde; kodlar <c>speech.*</c>).</summary>
public static class SpeechErrors
{
    /// <summary>Metin boş.</summary>
    /// <returns>Doğrulama hatası.</returns>
    public static Error EmptyText() => Error.Validation("speech.empty_text", "Seslendirilecek metin boş.");

    /// <summary>Çıktı yolu verilmedi.</summary>
    /// <returns>Doğrulama hatası.</returns>
    public static Error MissingOutput() => Error.Validation("speech.missing_output", "Çıktı ses dosyasının yolu belirtilmedi.");

    /// <summary>Dil desteklenmiyor.</summary>
    /// <param name="engine">Motor adı.</param>
    /// <param name="language">İstenen dil.</param>
    /// <returns>Doğrulama hatası.</returns>
    public static Error UnsupportedLanguage(string engine, string language) =>
        Error.Validation("speech.unsupported_language", $"{engine} '{language}' dilini desteklemiyor.");

    /// <summary>Hız aralık dışında.</summary>
    /// <param name="speed">İstenen hız.</param>
    /// <returns>Doğrulama hatası.</returns>
    public static Error InvalidSpeed(double speed) =>
        Error.Validation("speech.invalid_speed", $"Hız çarpanı 0.5 ile 2.0 arasında olmalı (verilen: {speed}).");

    /// <summary>Klonlama desteklenmiyor ya da referans ses yok.</summary>
    /// <param name="message">Açıklama.</param>
    /// <returns>Doğrulama hatası.</returns>
    public static Error Cloning(string message) => Error.Validation("speech.cloning", message);

    /// <summary>Ses (voice) bulunamadı.</summary>
    /// <param name="voiceId">Ses kimliği.</param>
    /// <returns>Bulunamadı hatası.</returns>
    public static Error VoiceNotFound(string voiceId) => Error.NotFound("speech.voice_not_found", $"'{voiceId}' sesi bulunamadı.");

    /// <summary>Motor kurulu değil.</summary>
    /// <param name="engine">Motor adı.</param>
    /// <returns>Hata.</returns>
    public static Error NotInstalled(string engine) =>
        Error.Failure("speech.not_installed", $"{engine} kurulu değil. Ön gereksinimler ekranından kurun.");

    /// <summary>İndirme başarısız.</summary>
    /// <param name="file">Dosya.</param>
    /// <param name="reason">Neden.</param>
    /// <returns>Hata.</returns>
    public static Error DownloadFailed(string file, string reason) =>
        Error.Failure("speech.download_failed", $"'{file}' indirilemedi: {reason}");

    /// <summary>Kurulum (venv / paket) başarısız.</summary>
    /// <param name="engine">Motor adı.</param>
    /// <param name="reason">Neden.</param>
    /// <returns>Hata.</returns>
    public static Error SetupFailed(string engine, string reason) =>
        Error.Failure("speech.setup_failed", $"{engine} kurulamadı: {reason}");

    /// <summary>Sentez başarısız.</summary>
    /// <param name="engine">Motor adı.</param>
    /// <param name="reason">Neden.</param>
    /// <returns>Hata.</returns>
    public static Error SynthesisFailed(string engine, string reason) =>
        Error.Failure("speech.synthesis_failed", $"{engine} seslendirme hatası: {reason}");

    /// <summary>Python işçisi hatası (başlatma, protokol, zaman aşımı).</summary>
    /// <param name="reason">Neden.</param>
    /// <returns>Hata.</returns>
    public static Error Worker(string reason) => Error.Failure("speech.worker", $"Python ses işçisi hatası: {reason}");
}
