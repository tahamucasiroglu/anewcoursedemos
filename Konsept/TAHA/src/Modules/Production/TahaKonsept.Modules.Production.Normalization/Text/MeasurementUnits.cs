namespace TahaKonsept.Modules.Production.Normalization.Text;

/// <summary>
/// Ölçü birimleri tablosu. <see cref="Free"/>: her yerde açılan, belirsizliği olmayan birimler (km, kg, cm, GB …);
/// <see cref="AfterNumber"/>: yalnız sayıdan sonra açılan belirsiz birimler (m, g, l, sn, dk, sa, ha, W, V …).
/// Sayı kuralı rakama bitişik birimleri (25km) ayırmak için <see cref="AllKeys"/> listesini kullanır.
/// </summary>
public static class MeasurementUnits
{
    /// <summary>Bağlamsız açılan birimler.</summary>
    public static IReadOnlyDictionary<string, string> Free { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["km/sa"] = "kilometre bölü saat", ["km/h"] = "kilometre bölü saat", ["km²"] = "kilometrekare",
        ["m²"] = "metrekare", ["m³"] = "metreküp", ["cm²"] = "santimetrekare", ["cm³"] = "santimetreküp",
        ["km"] = "kilometre", ["cm"] = "santimetre", ["kg"] = "kilogram", ["mg"] = "miligram", ["ml"] = "mililitre",
        ["GB"] = "gigabayt", ["MB"] = "megabayt", ["TB"] = "terabayt", ["KB"] = "kilobayt", ["GHz"] = "gigahertz",
        ["MHz"] = "megahertz", ["kWh"] = "kilovatsaat", ["kW"] = "kilovat", ["mAh"] = "miliamper saat",
    };

    /// <summary>Yalnız sayıdan sonra açılan birimler.</summary>
    public static IReadOnlyDictionary<string, string> AfterNumber { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["m/s"] = "metre bölü saniye", ["mm"] = "milimetre", ["m"] = "metre", ["gr"] = "gram", ["g"] = "gram",
        ["lt"] = "litre", ["l"] = "litre", ["sn"] = "saniye", ["dk"] = "dakika", ["sa"] = "saat", ["ms"] = "milisaniye",
        ["ha"] = "hektar", ["Hz"] = "hertz", ["W"] = "vat", ["V"] = "volt", ["mph"] = "mil bölü saat",
    };

    /// <summary>Tüm birim anahtarları, uzundan kısaya.</summary>
    public static IReadOnlyList<string> AllKeys { get; } =
        Free.Keys.Concat(AfterNumber.Keys).OrderByDescending(k => k.Length).ThenBy(k => k, StringComparer.Ordinal).ToList();
}
