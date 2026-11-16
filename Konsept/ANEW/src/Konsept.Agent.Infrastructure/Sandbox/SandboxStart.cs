using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Infrastructure.Sandbox;

/// <summary>Kısıtlı süreç başlatma isteği (spec 0018): ortam sıfırdan kurulur, yalnızca <see cref="Environment"/> geçer.</summary>
/// <param name="Executable">Yürütülebilir dosya.</param>
/// <param name="Arguments">Argümanlar.</param>
/// <param name="WorkingDirectory">Çalışma klasörü.</param>
/// <param name="Environment">Ortam değişkenlerinin tamamı.</param>
/// <param name="Limits">Sınırlar.</param>
public sealed record SandboxStart(
    string Executable,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment,
    RenderLimits Limits);
