using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Domain.Entities;

/// <summary>Kurulum motorunun ürettiği olay; ekran bunu Türkçe satıra çevirir (spec 0009 Req 5, 12, 17).</summary>
/// <param name="Kind">Olay türü.</param>
/// <param name="State">İlgili bileşenin durumu; genel olaylarda <c>null</c>.</param>
/// <param name="Attempt">Yeniden deneme sırası (1, 2).</param>
public sealed record SetupEvent(SetupEventKind Kind, ComponentState? State = null, int Attempt = 0);
