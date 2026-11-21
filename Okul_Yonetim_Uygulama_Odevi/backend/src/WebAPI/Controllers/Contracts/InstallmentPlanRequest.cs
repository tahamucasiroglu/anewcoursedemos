namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Taksit planı önizleme/açma (spec 0015): öğrenci, tarife, isteğe bağlı indirim, taksit sayısı (1–12) ve ilk vade (<c>yyyy-MM-dd</c>).</summary>
public sealed record InstallmentPlanRequest(Guid StudentId, Guid FeeScheduleId, Guid? DiscountId, int? InstallmentCount, DateOnly? FirstDueDate);
