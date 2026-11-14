namespace Konsept.Web.Tests.Support;

/// <summary>
/// Repo içindeki Web dosyalarının yolları (test çıktısından yukarı çıkarak bulunur).
/// </summary>
public static class WebPaths
{
    /// <summary>Gets Web projesinin klasörü.</summary>
    public static string WebProject
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Konsept.sln")))
            {
                dir = dir.Parent;
            }

            return dir is null
                ? throw new InvalidOperationException("Konsept.sln bulunamadı.")
                : Path.Combine(dir.FullName, "src", "Konsept.Web");
        }
    }
}
