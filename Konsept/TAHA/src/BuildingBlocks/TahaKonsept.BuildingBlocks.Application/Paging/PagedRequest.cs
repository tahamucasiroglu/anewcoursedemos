namespace TahaKonsept.BuildingBlocks.Application.Paging;

/// <summary>
/// Sayfalı liste isteği. <see cref="Page"/> 1'den başlar, <see cref="PageSize"/> 1–200 aralığına sıkıştırılır.
/// </summary>
public record PagedRequest
{
    /// <summary>En büyük sayfa boyutu.</summary>
    public const int MaxPageSize = 200;

    private readonly int _page = 1;
    private readonly int _pageSize = 20;

    /// <summary>Sayfa numarası (1'den başlar).</summary>
    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    /// <summary>Sayfa boyutu.</summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 1 : Math.Min(value, MaxPageSize);
    }

    /// <summary>Atlanacak kayıt sayısı.</summary>
    public int Skip => (Page - 1) * PageSize;
}
