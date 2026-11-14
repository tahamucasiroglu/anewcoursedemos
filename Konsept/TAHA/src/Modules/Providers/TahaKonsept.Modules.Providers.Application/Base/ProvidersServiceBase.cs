using TahaKonsept.BuildingBlocks.Application.Abstract;

namespace TahaKonsept.Modules.Providers.Application.Base;

/// <summary>
/// Providers modülü uygulama servislerinin ortak tabanı (saat; arayüz → bu taban → somut servis kalıbı).
/// </summary>
public abstract class ProvidersServiceBase : IApplicationService
{
    /// <summary>Kurucu.</summary>
    /// <param name="clock">Saat.</param>
    protected ProvidersServiceBase(IClock clock)
    {
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Saat (UTC).</summary>
    protected IClock Clock { get; }
}
