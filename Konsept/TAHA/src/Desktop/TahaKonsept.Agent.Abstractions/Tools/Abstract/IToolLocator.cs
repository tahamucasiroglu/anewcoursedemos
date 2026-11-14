using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Agent.Abstractions.Tools;

namespace TahaKonsept.Agent.Abstractions.Tools.Abstract;

/// <summary>
/// Harici araçların (node, ffmpeg, uv, sd.cpp…) yolunu bulur. Önce ajanın kendi kurulum klasörüne (<c>IAgentPaths.ToolsDir</c>),
/// sonra PATH'e bakar. Kurulum (indirme) bu arayüzün işi değildir; ön gereksinim yükleyicisi yapar.
/// </summary>
public interface IToolLocator
{
    /// <summary>Aracın tam yolunu döner; bulunamazsa <c>null</c>.</summary>
    /// <param name="kind">Araç.</param>
    /// <returns>Yol ya da <c>null</c>.</returns>
    string? Find(ToolKind kind);

    /// <summary>Aracın yolunu döner; yoksa anlaşılır bir hata (hangi bileşenin kurulması gerektiği) döner.</summary>
    /// <param name="kind">Araç.</param>
    /// <returns>Yol ya da hata.</returns>
    Result<string> Require(ToolKind kind);
}
