namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Bir aşama denemesinde başarılı ücretli çağrıların biriken maliyeti/token'ı (<see cref="StageExecutionRequest.Usage"/>). İşleyici her başarılı
/// çağrıdan sonra <see cref="Add"/> çağırır; aşama sonunda hata dönerse orkestratör bu harcamayı çalıştırmaya yazar (başarıda sonuçtaki değerler kullanılır).
/// </summary>
public sealed class StageUsageMeter
{
    /// <summary>Maliyet ($).</summary>
    public decimal CostUsd { get; private set; }

    /// <summary>Giriş token'ı.</summary>
    public long InputTokens { get; private set; }

    /// <summary>Çıkış token'ı.</summary>
    public long OutputTokens { get; private set; }

    /// <summary>Harcama ekler.</summary>
    /// <param name="costUsd">Maliyet.</param>
    /// <param name="inputTokens">Giriş token'ı.</param>
    /// <param name="outputTokens">Çıkış token'ı.</param>
    public void Add(decimal costUsd, long inputTokens = 0, long outputTokens = 0)
    {
        CostUsd += costUsd;
        InputTokens += inputTokens;
        OutputTokens += outputTokens;
    }
}
