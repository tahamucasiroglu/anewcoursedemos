using TahaKonsept.Agent.Shell.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Shell.Base;

/// <summary>
/// <see cref="IDesktopLauncher"/> tabanı: adres/klasör doğrulaması ve hata → <see cref="Result"/> çevirisi.
/// Asıl açma işlemi <see cref="Launch"/> ile türetilen sınıftadır.
/// </summary>
public abstract class DesktopLauncherBase : IDesktopLauncher
{
    /// <inheritdoc />
    public Result OpenUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Result.Failure(Error.Validation("launcher.invalid_url", "Geçersiz adres: " + url));
        }

        return SafeLaunch(uri.AbsoluteUri, null);
    }

    /// <inheritdoc />
    public Result OpenFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure(Error.Validation("launcher.invalid_path", "Klasör yolu boş."));
        }

        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Failure(Error.Failure("launcher.folder_failed", "Klasör oluşturulamadı: " + ex.Message));
        }

        return SafeLaunch("explorer.exe", "\"" + path + "\"");
    }

    /// <summary>İşletim sistemi kabuğuyla açar.</summary>
    /// <param name="target">Adres ya da program.</param>
    /// <param name="arguments">Argümanlar (yoksa <c>null</c>).</param>
    protected abstract void Launch(string target, string? arguments);

    private Result SafeLaunch(string target, string? arguments)
    {
        try
        {
            Launch(target, arguments);
            return Result.Success();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return Result.Failure(Error.Failure("launcher.failed", "Açılamadı: " + ex.Message));
        }
    }
}
