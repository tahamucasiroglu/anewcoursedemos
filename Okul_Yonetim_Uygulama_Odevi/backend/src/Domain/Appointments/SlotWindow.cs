namespace OkulYonetim.Domain.Appointments;

/// <summary>Bir görüşme diliminin zaman aralığı (spec 0016 C-1): başlangıç dahil, bitiş hariç; ikisi de UTC.</summary>
public readonly record struct SlotWindow(DateTimeOffset StartsAt, DateTimeOffset EndsAt)
{
    /// <summary>İki aralık kesişiyor mu (bitişik aralıklar kesişmez).</summary>
    public bool Overlaps(DateTimeOffset startsAt, DateTimeOffset endsAt) => this.StartsAt < endsAt && startsAt < this.EndsAt;
}
