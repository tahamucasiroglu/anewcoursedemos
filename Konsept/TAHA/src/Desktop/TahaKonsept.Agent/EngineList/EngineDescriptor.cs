using TahaKonsept.Agent.Abstractions.Engines.Abstract;

namespace TahaKonsept.Agent.EngineList;

/// <summary>Durum penceresinde listelenen bir yerel motor ve grubu.</summary>
/// <param name="Engine">Motor.</param>
/// <param name="Category">Grup.</param>
public sealed record EngineDescriptor(IAgentEngine Engine, EngineCategory Category);
