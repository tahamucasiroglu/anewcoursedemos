using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Execution;

/// <summary>Kuyruktaki aşama: komut ve gönderildiği andaki iş denetimi (duraklat sonrası eski kuyruk öğeleri yeni denetime karışmaz).</summary>
/// <param name="Command">Komut.</param>
/// <param name="Control">İş denetimi.</param>
public sealed record QueuedStage(StageCommand Command, JobControl Control);
