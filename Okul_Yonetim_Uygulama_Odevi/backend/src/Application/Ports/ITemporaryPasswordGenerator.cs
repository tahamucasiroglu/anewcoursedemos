namespace OkulYonetim.Application.Ports;

/// <summary>
/// Geçici şifre üreteci (spec 0005, Ö-1): 12 karakter, harf ve rakam karışık, birbirine benzeyen 0/O/1/l/I yok;
/// güvenli rastgele kaynakla üretilir. Üretilen şifre yalnızca bir kez yanıtla gösterilir; hiçbir yerde saklanmaz.
/// </summary>
public interface ITemporaryPasswordGenerator
{
    string Generate();
}
