using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>Bekleyen bildirimlerin ajandaki kalıcı yeri (spec 0022 Req 7): ajan yeniden başlasa da kaybolmaz; içerik maskelidir.</summary>
public interface IErrorReportQueue
{
    /// <summary>Saklı durumu okur (yoksa ya da bozuksa boş).</summary>
    /// <returns>Durum.</returns>
    PendingReportState Load();

    /// <summary>Durumu saklar.</summary>
    /// <param name="state">Durum.</param>
    void Save(PendingReportState state);
}
