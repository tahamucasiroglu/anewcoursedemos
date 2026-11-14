using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Identity modülünün iş birimi (Identity DbContext). Genel <see cref="IUnitOfWork"/>'ten ayrı tutulur ki
/// diğer modüllerin DbContext kayıtlarıyla çakışmasın.
/// </summary>
public interface IIdentityUnitOfWork : IUnitOfWork
{
}
