using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Base;

namespace TahaKonsept.Modules.Profiles.Application.Concrete;

/// <summary>
/// Varsayılan profil okuyucu (diğer modüller <see cref="IProfileReader"/> üzerinden kullanır).
/// </summary>
/// <param name="repository">Profil deposu.</param>
public sealed class ProfileReader(IProfileRepository repository) : ProfileReaderBase(repository);
