namespace Konsept.Agent.Domain.Entities;

/// <summary>Ön kontrol sonucu (spec 0009 Req 10): internet ve disk alanı.</summary>
/// <param name="Online">İnternet bağlantısı var mı.</param>
/// <param name="RequiredBytes">Gereken alan (indirme + açılmış × 1,2).</param>
/// <param name="FreeBytes">Kurulum yerindeki boş alan.</param>
public sealed record SetupPreflight(bool Online, long RequiredBytes, long FreeBytes)
{
    /// <summary>Gets a value indicating whether disk alanı yeterli.</summary>
    public bool EnoughSpace => FreeBytes >= RequiredBytes;

    /// <summary>Gets a value indicating whether ön kontrol geçti.</summary>
    public bool Passed => Online && EnoughSpace;
}
