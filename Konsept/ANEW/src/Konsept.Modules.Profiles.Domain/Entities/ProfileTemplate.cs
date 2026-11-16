namespace Konsept.Modules.Profiles.Domain.Entities;

/// <summary>Yeni profil modalındaki hazır şablon (spec 0003 Req 5): tüm kullanıcılar için aynı, değiştirilemez.</summary>
/// <param name="Id">Kararlı kimlik.</param>
/// <param name="Title">Şablonun görünen adı (ör. "Tarih – Bizans").</param>
/// <param name="ChannelName">Örnek kanal adı.</param>
/// <param name="Language">Video dili.</param>
/// <param name="Format">Biçim.</param>
/// <param name="DurationSeconds">Hedef süre (saniye).</param>
/// <param name="Content">Örnek kanal içeriği.</param>
public sealed record ProfileTemplate(string Id, string Title, string ChannelName, string Language, string Format, int DurationSeconds, string Content);
