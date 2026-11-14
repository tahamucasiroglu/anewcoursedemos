using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Execution;

/// <summary>
/// Bir aşama yürütmesinin bağlamı: komut, iş klasörü, indirilen girdiler (ad → yol) ve ilerleme bildirimi.
/// Önemli üyeler: <see cref="Report"/>, <see cref="FindInput"/>, <see cref="GetSetting"/>.
/// </summary>
public sealed class StageExecutionContext
{
    private readonly Action<double, string>? _report;

    /// <summary>Oluşturur.</summary>
    /// <param name="command">Aşama komutu.</param>
    /// <param name="jobDir">İş klasörü.</param>
    /// <param name="inputs">İndirilen girdiler (mantıksal ad → yerel yol).</param>
    /// <param name="report">İlerleme geri çağrısı (yüzde, mesaj).</param>
    public StageExecutionContext(StageCommand command, string jobDir, IReadOnlyDictionary<string, string>? inputs, Action<double, string>? report)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        ArgumentException.ThrowIfNullOrWhiteSpace(jobDir);
        JobDir = jobDir;
        Inputs = inputs is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(inputs, StringComparer.OrdinalIgnoreCase);
        _report = report;
    }

    /// <summary>Aşama komutu.</summary>
    public StageCommand Command { get; }

    /// <summary>İş klasörü.</summary>
    public string JobDir { get; }

    /// <summary>İndirilen girdiler (büyük/küçük harf duyarsız ad → yol).</summary>
    public IReadOnlyDictionary<string, string> Inputs { get; }

    /// <summary>İlerleme bildirir (0–100 aralığına sıkıştırılır).</summary>
    /// <param name="percent">Yüzde.</param>
    /// <param name="message">Türkçe mesaj.</param>
    public void Report(double percent, string message) => _report?.Invoke(Math.Clamp(percent, 0d, 100d), message);

    /// <summary>İndirilen girdiyi adıyla bulur (dosya var olmalı).</summary>
    /// <param name="name">Mantıksal ad.</param>
    /// <returns>Yol ya da <c>null</c>.</returns>
    public string? FindInput(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return Inputs.TryGetValue(Path.GetFileName(name), out var path) && File.Exists(path) ? path : null;
    }

    /// <summary>Aşama ayarını okur.</summary>
    /// <param name="key">Anahtar.</param>
    /// <returns>Değer ya da <c>null</c>.</returns>
    public string? GetSetting(string key) =>
        Command.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
