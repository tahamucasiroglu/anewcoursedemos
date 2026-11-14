namespace TahaKonsept.Agent.Engines.Speech.Onnx;

/// <summary>
/// <see cref="Abstract.IOnnxSessionFactory.Load{T}"/> sonucu: açılan nesne (<see cref="Value"/>, ör. Supertonic'in dört oturumu),
/// kullanılan sağlayıcı (<see cref="Provider"/>) ve GPU istendiği hâlde CPU'ya düşüldüyse nedeni (<see cref="FallbackReason"/>).
/// </summary>
/// <typeparam name="T">Açılan nesnenin türü.</typeparam>
/// <param name="Value">Açılan nesne.</param>
/// <param name="Provider">Kullanılan sağlayıcı.</param>
/// <param name="FallbackReason">GPU istendi ama CPU kullanıldıysa nedeni; aksi hâlde <c>null</c>.</param>
public sealed record OnnxLoadResult<T>(T Value, OnnxExecutionProvider Provider, string? FallbackReason);
