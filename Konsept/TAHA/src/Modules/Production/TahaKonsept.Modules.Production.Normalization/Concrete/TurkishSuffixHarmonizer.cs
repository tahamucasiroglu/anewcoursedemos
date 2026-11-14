using TahaKonsept.Modules.Production.Normalization.Base;

namespace TahaKonsept.Modules.Production.Normalization.Concrete;

/// <summary>
/// Varsayılan Türkçe ek uyumlayıcı (algoritma <see cref="TurkishSuffixHarmonizerBase"/> içinde).
/// Örnekler: ("üç", "te") → "üçte", ("kırk", "ta") → "kırkta", ("dört", "ü") → "dördü", ("lira", "ye") → "liraya".
/// </summary>
public sealed class TurkishSuffixHarmonizer : TurkishSuffixHarmonizerBase
{
}
