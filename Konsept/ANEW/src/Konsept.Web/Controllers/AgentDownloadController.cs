using System.Globalization;
using System.Reflection;
using Konsept.Web.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// "Ajanı indir" (spec 0009 Req 1, AC-1): giriş yapmış kullanıcıya Pi'den ürünün güncel sürümündeki tek kurulum dosyasını verir.
/// Dosya deploy'un yayımladığı klasördedir (<c>AgentDownload:Directory</c>; Pi'de <c>/opt/anewconcept/agent</c>) ve adı ürün
/// sürümünü taşır. Giriş ister (default-deny); dosya yoksa Türkçe 404.
/// </summary>
public sealed class AgentDownloadController : Controller
{
    private readonly IConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="AgentDownloadController"/> class.</summary>
    /// <param name="configuration">Yapılandırma.</param>
    public AgentDownloadController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Gets ürün sürümü (Web, Api ve ajan ortak; derleme sonekleri olmadan).</summary>
    public static string ProductVersion { get; } =
        (typeof(AgentDownloadController).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
            .Split('+')[0];

    /// <summary>Gets bu sürümün kurulum dosyasının adı (deploy aynı adla yayımlar: <c>anewconcept-ajan-&lt;sürüm&gt;-win-x64.exe</c>).</summary>
    public static string CurrentFileName { get; } = string.Create(CultureInfo.InvariantCulture, $"anewconcept-ajan-{ProductVersion}-win-x64.exe");

    /// <summary>Kurulum dosyasını indirir.</summary>
    /// <returns>Dosya ya da 404.</returns>
    [HttpGet("/ajan/indir")]
    public IActionResult Download()
    {
        var directory = _configuration["AgentDownload:Directory"];
        var path = string.IsNullOrEmpty(directory) ? null : Path.Combine(directory, CurrentFileName);
        if (path is null || !System.IO.File.Exists(path))
        {
            return new ContentResult
            {
                StatusCode = StatusCodes.Status404NotFound,
                Content = UiText.AgentDownloadMissing,
                ContentType = "text/plain; charset=utf-8",
            };
        }

        return PhysicalFile(path, "application/vnd.microsoft.portable-executable", CurrentFileName, enableRangeProcessing: true);
    }
}
