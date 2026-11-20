namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Gönderilen mesaj ve öğretmenin mesai saatleri dışında mı (veli "mesajınız iletildi" bilgisini görür).</summary>
public sealed record MessageSent(MessageView Message, bool OutsideWorkingHours);
