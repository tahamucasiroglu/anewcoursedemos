namespace TahaKonsept.Web.Models;

/// <summary>Kayıt sayfasının görünüm modeli: <see cref="RegistrationOpen"/> false ise form yerine "kayıtlar kapalı" mesajı gösterilir.</summary>
public sealed record RegisterPageModel
{
    /// <summary>Yeni kayıt açık mı.</summary>
    public bool RegistrationOpen { get; init; } = true;
}
