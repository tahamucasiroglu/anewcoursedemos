using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Sağlık notu ve alerjinin şifrelenmesi/çözülmesi (spec 0009 gereksinim 9). Düz metin yalnızca bu sınıf ile istek/yanıt
/// arasında dolaşır; loglara yalnızca öğrenci kimliği yazılır. Anahtar ayarlı değilse okumada "gösterilemiyor"
/// (<c>Available</c> false), dolu sağlık bilgisi kaydetmede "kaydedilemiyor" (iş kuralı hatası) ve sunucu logunda
/// "yapılandırılmamış" hatası. Çözülemeyen veri (anahtar değişmiş/bozuk) "gösterilemiyor" sayılır ve uyarı loglanır.
/// </summary>
public sealed class StudentHealth
{
    private readonly IPersonalDataProtector protector;
    private readonly ILocalizationService localization;
    private readonly ILogger<StudentHealth> logger;

    public StudentHealth(IPersonalDataProtector protector, ILocalizationService localization, ILogger<StudentHealth> logger)
    {
        this.protector = protector;
        this.localization = localization;
        this.logger = logger;
    }

    public static bool HasAny(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        return student.HealthNoteCipher is not null || student.AllergyCipher is not null;
    }

    public StudentHealthView Read(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        if (!this.protector.IsConfigured)
        {
            return new StudentHealthView(false, null, null);
        }

        try
        {
            return new StudentHealthView(true, this.Decrypt(student.HealthNoteCipher), this.Decrypt(student.AllergyCipher));
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            this.logger.LogWarning("Öğrenci {StudentId} sağlık bilgisi çözülemedi; gösterilemiyor", student.Id);
            return new StudentHealthView(false, null, null);
        }
    }

    /// <summary>Boş alanlar <c>null</c>; dolu alan varsa ve anahtar yoksa iş kuralı hatası (log: "yapılandırılmamış").</summary>
    public async Task<(string? NoteCipher, string? AllergyCipher)> ProtectAsync(string? healthNote, string? allergy, Guid? studentId)
    {
        string? note = Clean(healthNote);
        string? allergyText = Clean(allergy);
        if (note is null && allergyText is null)
        {
            return (null, null);
        }

        if (!this.protector.IsConfigured)
        {
            this.logger.LogError("Sağlık bilgisi kaydedilemedi (öğrenci {StudentId}): şifreleme anahtarı yapılandırılmamış (PersonalData:EncryptionKey)", studentId);
            throw new BusinessException(await this.localization.StudentTextAsync(StudentsMessages.HealthUnavailable).ConfigureAwait(false));
        }

        return (note is null ? null : this.protector.Protect(note), allergyText is null ? null : this.protector.Protect(allergyText));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string? Decrypt(string? cipher) => cipher is null ? null : this.protector.Unprotect(cipher);
}
