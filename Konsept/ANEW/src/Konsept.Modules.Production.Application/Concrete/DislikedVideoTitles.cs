using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Base;

namespace Konsept.Modules.Production.Application.Concrete;

/// <summary>"Beğenilmedi" negatif sinyalinin kaynağı (spec 0019 Req 6); davranış <see cref="DislikedVideoTitlesBase"/>'dedir.</summary>
/// <param name="videos">Sürüm kayıtları.</param>
public sealed class DislikedVideoTitles(IProducedVideoRepository videos) : DislikedVideoTitlesBase(videos);
