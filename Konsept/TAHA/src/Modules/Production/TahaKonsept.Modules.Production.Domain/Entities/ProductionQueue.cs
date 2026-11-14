using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Domain.Entities;

/// <summary>
/// Kullanıcının üretim sırasının çalışma durumu (kullanıcı başına tek kayıt): <see cref="RunState"/> (Boşta / Çalışıyor / Durduruldu),
/// genel mesaj ve bütçe engeli. Öğeler ayrı aggregate'tir (<see cref="QueueItem"/>). Önemli metotlar: <see cref="Start"/>,
/// <see cref="Pause"/>, <see cref="Resume"/>, <see cref="Stop"/>.
/// </summary>
public sealed class ProductionQueue : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private ProductionQueue()
    {
    }

    /// <summary>Sahip kullanıcı.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Çalışma durumu.</summary>
    public QueueRunState RunState { get; private set; } = QueueRunState.Idle;

    /// <summary>Genel mesaj.</summary>
    public string? Message { get; private set; }

    /// <summary>Bütçe aşıldığı için durdu mu.</summary>
    public bool BudgetBlocked { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Boşta bekleyen sıra oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Sıra.</returns>
    public static ProductionQueue Create(Guid userId) => new() { UserId = userId };

    /// <summary>Sırayı başlatır (bütçe engeli ve mesaj temizlenir).</summary>
    public void Start()
    {
        RunState = QueueRunState.Running;
        BudgetBlocked = false;
        Message = null;
    }

    /// <summary>Sırayı durdurur (mevcut iş kontrol noktasında bekler).</summary>
    public void Pause()
    {
        RunState = QueueRunState.Paused;
        Message = "Durduruldu";
    }

    /// <summary>Durdurulan sırayı kaldığı yerden sürdürür.</summary>
    public void Resume() => Start();

    /// <summary>Sırayı boşta durumuna alır.</summary>
    /// <param name="message">Mesaj.</param>
    /// <param name="budgetBlocked">Bütçe nedeniyle mi.</param>
    public void Stop(string? message, bool budgetBlocked = false)
    {
        RunState = QueueRunState.Idle;
        Message = message;
        BudgetBlocked = budgetBlocked;
    }
}
