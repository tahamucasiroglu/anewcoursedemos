using System.Reflection;

namespace TahaKonsept.BuildingBlocks.Application.Common;

/// <summary>
/// Uygulama sürümünü (kökteki VERSION dosyasından derlemeye yazılan) okur.
/// </summary>
public static class AppVersion
{
    /// <summary>Verilen derlemenin bilgi sürümünü döner (ör. <c>0.1.0</c>); <c>+commit</c> eki atılır.</summary>
    /// <param name="assembly">Derleme.</param>
    /// <returns>Sürüm metni.</returns>
    public static string Of(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational))
        {
            return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }

        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? informational[..plus] : informational;
    }
}
