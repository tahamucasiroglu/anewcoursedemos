namespace OkulYonetim.Application.Features.Communication;

/// <summary>Hoş geldin ekranının sayıları (spec 0011): velinin okunmamış duyuruları ve kişinin okunmamış mesajları.</summary>
public sealed record CommunicationUnread(int Announcements, int Messages);
