namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Konuşmadaki mesaj: benim mi, metin ve zaman.</summary>
public sealed record MessageView(Guid Id, bool Mine, string Body, DateTimeOffset SentAt);
