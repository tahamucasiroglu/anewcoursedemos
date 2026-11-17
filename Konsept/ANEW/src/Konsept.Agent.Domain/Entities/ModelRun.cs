namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Yerel modelin çalıştırma şablonu (spec 0028 Req 7, 8): görev türü, çalıştırılacak aracın bileşeni ve argüman listesi. Argümanlardaki
/// yer tutucular (<see cref="Rules.ModelRunTemplate"/>) çalıştırma anında çözülür; kullanıcı metni yalnızca argüman değeri olur.
/// </summary>
/// <param name="Task">Görev: <see cref="Image"/> ya da <see cref="Speech"/>.</param>
/// <param name="Tool">Yürütülebilir dosyası çalıştırılan bileşen (ör. <c>sdcpp</c>, <c>python</c>).</param>
/// <param name="Args">Argüman şablonu.</param>
/// <param name="TimeoutSeconds">Tek çalıştırmanın en uzun süresi (görselde sahne başına, seste tüm aşama).</param>
public sealed record ModelRun(string Task, string Tool, IReadOnlyList<string> Args, int TimeoutSeconds)
{
    /// <summary>Görev: metinden görsel.</summary>
    public const string Image = "image";

    /// <summary>Görev: metinden konuşma.</summary>
    public const string Speech = "speech";
}
