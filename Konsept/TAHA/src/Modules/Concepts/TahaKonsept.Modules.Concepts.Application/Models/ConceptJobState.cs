using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// İş izleyicisinin bir iş için tuttuğu değişken durum: iş, son durum DTO'su (kilit altında değiştirilir) ve iptal kaynağı.
/// </summary>
public sealed class ConceptJobState : IDisposable
{
    /// <summary>Oluşturur.</summary>
    /// <param name="job">İş.</param>
    /// <param name="dto">İlk durum.</param>
    public ConceptJobState(ConceptJob job, ConceptJobDto dto)
    {
        Job = job ?? throw new ArgumentNullException(nameof(job));
        Dto = dto ?? throw new ArgumentNullException(nameof(dto));
    }

    /// <summary>İş.</summary>
    public ConceptJob Job { get; }

    /// <summary>Kilit nesnesi.</summary>
    public Lock Gate { get; } = new();

    /// <summary>Son durum.</summary>
    public ConceptJobDto Dto { get; set; }

    /// <summary>İptal kaynağı.</summary>
    public CancellationTokenSource Cancellation { get; } = new();

    /// <summary>Bitti mi (tamamlandı / başarısız / iptal).</summary>
    public bool IsFinished => Dto.Status is ConceptJobStatus.Completed or ConceptJobStatus.Failed or ConceptJobStatus.Cancelled;

    /// <summary>İptal kaynağını bırakır.</summary>
    public void Dispose() => Cancellation.Dispose();
}
