using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Üretim sırası köprüsü (Concepts.Contracts, spec 0006).</summary>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="time">Saat.</param>
public sealed class ConceptQueueBridge(IConceptRepository concepts, TimeProvider time) : ConceptQueueBridgeBase(concepts, time);
