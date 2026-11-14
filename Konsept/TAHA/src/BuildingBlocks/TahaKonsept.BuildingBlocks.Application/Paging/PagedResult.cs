namespace TahaKonsept.BuildingBlocks.Application.Paging;

/// <summary>
/// Sayfalı liste sonucu: öğeler, toplam kayıt, sayfa bilgisi ve türetilmiş <see cref="TotalPages"/>.
/// </summary>
/// <typeparam name="T">Öğe türü.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>Bu sayfadaki öğeler.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Toplam kayıt sayısı.</summary>
    public int TotalCount { get; init; }

    /// <summary>Sayfa numarası.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu.</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>Toplam sayfa sayısı.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Öğeleri başka türe dönüştürür (sayfa bilgisi korunur).</summary>
    /// <typeparam name="TOut">Hedef tür.</typeparam>
    /// <param name="map">Dönüştürücü.</param>
    /// <returns>Yeni sayfa.</returns>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> map) => new()
    {
        Items = Items.Select(map).ToList(),
        TotalCount = TotalCount,
        Page = Page,
        PageSize = PageSize,
    };
}
