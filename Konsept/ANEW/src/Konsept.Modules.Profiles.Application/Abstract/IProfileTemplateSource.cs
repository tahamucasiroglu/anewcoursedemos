using Konsept.Modules.Profiles.Domain.Entities;

namespace Konsept.Modules.Profiles.Application.Abstract;

/// <summary>Hazır şablonların kaynağı (sistemle gelir; kullanıcı değiştiremez, Req 5).</summary>
public interface IProfileTemplateSource
{
    /// <summary>Gets şablonlar (gösterim sırasıyla).</summary>
    IReadOnlyList<ProfileTemplate> Templates { get; }
}
