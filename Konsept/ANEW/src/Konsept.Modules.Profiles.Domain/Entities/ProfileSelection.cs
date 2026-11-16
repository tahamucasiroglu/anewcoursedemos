namespace Konsept.Modules.Profiles.Domain.Entities;

/// <summary>Kullanıcının sunucuda hatırlanan "son seçilen profil"i (spec 0003 Req 9); kullanıcı başına tek satır.</summary>
public sealed class ProfileSelection
{
    /// <summary>Gets kullanıcı.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets seçili profil (silinmişse okunurken ilk oluşturulana düşülür).</summary>
    public Guid ProfileId { get; init; }
}
