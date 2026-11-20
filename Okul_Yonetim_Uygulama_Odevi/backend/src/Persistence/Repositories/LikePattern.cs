namespace OkulYonetim.Persistence.Repositories;

/// <summary>ILIKE için desen kaçışı: değerdeki joker karakterler (\ % _) düz karakter sayılır ("%" adı her kaydı eşlemez).</summary>
public static class LikePattern
{
    public static string Escape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }

    public static string Contains(string value) => "%" + Escape(value) + "%";
}
