namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>İsteği yapan kullanıcının sunucuda kurulmuş yetkileri (Api principal'ındaki <c>konsept.perm</c> claim'leri).</summary>
public interface ICallerPermissions
{
    /// <summary>Gets yetkiler; giriş yoksa boş.</summary>
    IReadOnlyCollection<string> Current { get; }
}
