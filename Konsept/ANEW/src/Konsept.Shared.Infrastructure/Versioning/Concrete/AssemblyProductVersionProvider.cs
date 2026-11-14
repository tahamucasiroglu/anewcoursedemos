using System.Reflection;
using Konsept.Shared.Infrastructure.Versioning.Base;

namespace Konsept.Shared.Infrastructure.Versioning.Concrete;

/// <summary>
/// Ürün versiyonunu derlemenin <see cref="AssemblyInformationalVersionAttribute"/> bilgisinden okur;
/// bu bilgi <c>Directory.Build.props</c> içindeki tek <c>Version</c> değerinden gelir.
/// </summary>
public sealed class AssemblyProductVersionProvider : ProductVersionProviderBase
{
    private readonly Assembly _assembly;

    /// <summary>Initializes a new instance of the <see cref="AssemblyProductVersionProvider"/> class.</summary>
    /// <param name="assembly">Versiyonu taşıyan derleme.</param>
    public AssemblyProductVersionProvider(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    /// <inheritdoc />
    protected override string? ReadRawVersion() =>
        _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
