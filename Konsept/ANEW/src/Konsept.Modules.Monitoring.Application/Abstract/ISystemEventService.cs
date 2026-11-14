using Konsept.Modules.Monitoring.Contracts.Abstract;

namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>
/// Olay kaydının yazma ve okuma servisi; Contracts'taki iki yüzeyi tek serviste toplar.
/// </summary>
public interface ISystemEventService : ISystemEventWriter, ISystemEventReader
{
}
