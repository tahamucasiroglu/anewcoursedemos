namespace Konsept.Modules.AgentGateway.Domain.Entities;

/// <summary>
/// Ajan log özetleri için dakikalık kota (plan 0008, Risk 6): bir dakikalık pencerede en fazla
/// <see cref="PerMinute"/> özet kabul edilir, fazlası düşer. İş parçacığı güvenlidir.
/// </summary>
public sealed class AgentLogQuota
{
    /// <summary>Dakikada kabul edilen en fazla özet.</summary>
    public const int PerMinute = 60;

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly Lock _gate = new();
    private DateTimeOffset _windowStart = DateTimeOffset.MinValue;
    private int _count;

    /// <summary>Kotadan bir hak kullanmayı dener.</summary>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Hak varsa <c>true</c>; pencere doluysa <c>false</c>.</returns>
    public bool TryConsume(DateTimeOffset now)
    {
        lock (_gate)
        {
            if (now - _windowStart >= Window)
            {
                _windowStart = now;
                _count = 0;
            }

            if (_count >= PerMinute)
            {
                return false;
            }

            _count++;
            return true;
        }
    }
}
