namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Onay cevabı (spec 0011): öğrenci ve cevap (<c>Approved</c> = Onaylıyorum, <c>Declined</c> = Onaylamıyorum).</summary>
public sealed record ConsentAnswerRequest(Guid? StudentId, string? Answer);
