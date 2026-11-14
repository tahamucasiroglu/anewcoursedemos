namespace TahaKonsept.Agent.Account.Abstract;

/// <summary>Bu bilgisayarın eşlendiği hesabın görünen adını verir (durum penceresi gösterir).</summary>
public interface IPairedAccountReader
{
    /// <summary>Eşlenen hesabın kullanıcı adı; eşleme yoksa ya da okunamıyorsa <c>null</c>.</summary>
    /// <returns>Kullanıcı adı ya da <c>null</c>.</returns>
    string? GetAccountName();
}
