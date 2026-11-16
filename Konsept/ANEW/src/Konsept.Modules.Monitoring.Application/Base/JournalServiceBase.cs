using System.Globalization;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Modules.Monitoring.Domain.Rules;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Base;

/// <summary>
/// Günlük geçmişinin ortak mantığı: gün çözümü (Europe/Istanbul; ileri/geçersiz → bugün, ilk günden önce → ilk gün), filtre,
/// 50'lik sayfa (+1 kayıt okunarak "devamı var mı"). İlk gün = min(hesabın açıldığı gün, en eski kaydın günü) — plan Risk 2.
/// Sahiplik: kullanıcı kimliği yalnızca oturumdan gelir (BR-1); kalıcılık alt sınıftadır.
/// </summary>
/// <param name="timeProvider">Saat kaynağı.</param>
public abstract class JournalServiceBase(TimeProvider timeProvider) : IJournalService
{
    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; } = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc />
    public async Task<Result<JournalDayDto>> GetDayAsync(JournalDayRequest? request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
        {
            return Result<JournalDayDto>.Failure(MonitoringErrors.SignInRequired);
        }

        var today = JournalDay.DayOf(TimeProvider.GetUtcNow());
        var firstDay = await FindFirstDayAsync(userId, today, cancellationToken).ConfigureAwait(false);
        var day = JournalDay.Resolve(request?.Date, today, firstDay);
        var (fromUtc, toUtc) = JournalDay.RangeOf(day);
        var levels = JournalDay.LevelsOf(JournalDay.ParseFilter(request?.Filter));
        var skip = Math.Max(0, request?.Skip ?? 0);

        var page = await ListAsync(userId, fromUtc, toUtc, levels, skip, JournalDay.PageSize + 1, cancellationToken).ConfigureAwait(false);
        var items = page.Take(JournalDay.PageSize).Select(SystemEventServiceBase.ToDto).ToList();
        return Result<JournalDayDto>.Success(new JournalDayDto(
            day.ToString(JournalDay.DateFormat, CultureInfo.InvariantCulture),
            day == today,
            day <= firstDay,
            items,
            page.Count > JournalDay.PageSize));
    }

    /// <summary>Gets isteği yapan kullanıcı; giriş yapılmamışsa <c>null</c>.</summary>
    protected abstract Guid? CurrentUserId { get; }

    /// <summary>Hesabın açıldığı anı okur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>An ya da <c>null</c>.</returns>
    protected abstract Task<DateTimeOffset?> ReadRegisteredAtAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının en eski kaydının anını okur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>An ya da <c>null</c>.</returns>
    protected abstract Task<DateTimeOffset?> ReadEarliestAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının aralıktaki kayıtlarını en yenisi üstte okur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="fromUtc">Aralık başı.</param>
    /// <param name="toUtc">Aralık sonu (hariç).</param>
    /// <param name="levels">Seviyeler; <c>null</c> ise hepsi.</param>
    /// <param name="skip">Atlanacak.</param>
    /// <param name="take">Alınacak.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    protected abstract Task<IReadOnlyList<SystemEvent>> ListAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyList<EventLevel>? levels,
        int skip,
        int take,
        CancellationToken cancellationToken);

    private async Task<DateOnly> FindFirstDayAsync(Guid userId, DateOnly today, CancellationToken cancellationToken)
    {
        var first = today;
        if (await ReadRegisteredAtAsync(userId, cancellationToken).ConfigureAwait(false) is { } registeredAt)
        {
            first = Min(first, JournalDay.DayOf(registeredAt));
        }

        if (await ReadEarliestAsync(userId, cancellationToken).ConfigureAwait(false) is { } earliest)
        {
            first = Min(first, JournalDay.DayOf(earliest));
        }

        return first;
    }

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;
}
