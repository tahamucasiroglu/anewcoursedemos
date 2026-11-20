namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Konuşma başlatma (spec 0011): öğrenci ve karşı taraf (veli için öğretmen, öğretmen için veli).</summary>
public sealed record StartConversationRequest(Guid? StudentId, Guid? CounterpartUserId);
