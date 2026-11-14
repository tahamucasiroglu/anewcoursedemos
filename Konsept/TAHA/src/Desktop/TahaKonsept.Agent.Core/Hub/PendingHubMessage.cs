namespace TahaKonsept.Agent.Core.Hub;

/// <summary>Bağlantı yokken kuyruğa alınan, yeniden bağlanınca gönderilecek hub çağrısı (tamamlandı/başarısız raporları).</summary>
/// <param name="Method">Hub metodu adı (<c>IAgentHub</c>).</param>
/// <param name="Argument">Argüman.</param>
public sealed record PendingHubMessage(string Method, object Argument);
