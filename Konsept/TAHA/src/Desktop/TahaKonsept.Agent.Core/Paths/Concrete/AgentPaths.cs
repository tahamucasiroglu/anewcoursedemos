using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Paths.Base;

namespace TahaKonsept.Agent.Core.Paths.Concrete;

/// <summary>
/// Ajan klasörleri: kök <see cref="AgentOptions.RootDirectory"/> ya da <c>%LocalAppData%/TahaKonsept</c>.
/// </summary>
public sealed class AgentPaths : AgentPathsBase
{
    /// <summary>Uygulama klasör adı (LocalAppData altında).</summary>
    public const string ProductFolderName = "TahaKonsept";

    /// <summary>Seçeneklerden kökü çözer.</summary>
    /// <param name="options">Seçenekler.</param>
    public AgentPaths(AgentOptions options)
        : base(ResolveRoot(options))
    {
    }

    /// <summary>Kök klasörü çözer.</summary>
    /// <param name="options">Seçenekler.</param>
    /// <returns>Kök yol.</returns>
    public static string ResolveRoot(AgentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return string.IsNullOrWhiteSpace(options.RootDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductFolderName)
            : options.RootDirectory;
    }
}
