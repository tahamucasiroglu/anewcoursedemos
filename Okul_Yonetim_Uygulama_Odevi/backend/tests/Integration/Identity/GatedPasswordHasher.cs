using System.Diagnostics;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Security;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>
/// Eşzamanlılık testi için gerçek şifre doğrulayıcının sarmalayıcısı (review H-1): doğru şifrenin doğrulaması,
/// <see cref="WrongAttemptsDone"/> işaretlenene kadar (en fazla <see cref="Wait"/>) bekletilir. Böylece "doğru şifre
/// kilit kontrolünü geçtikten sonra yanlış denemeler hesabı kilitler" sırası zorlanır. Bekleme bitince işaretin o anki
/// durumu <see cref="WrongAttemptsFinishedWhileCorrectWasVerifying"/>'e yazılır.
/// <para>
/// <see cref="QueuedAttempts"/> verilirse bekleme, yanlış denemelerin <see cref="ExpectedWrongAttempts"/> tanesi aynı
/// e-postanın giriş sırasında beklerken görülünce de biter (0011 VERIFY V-1): sıralı kodda yanlışlar doğru isteğin bitmesini
/// bekler, sabit 3 sn tutmak onların 5 sn'lik sıra süresinden (lock_timeout) yalnızca 2 sn bırakıyordu; yük altında yanlış
/// denemeler "çok fazla istek" alıp sayılmıyor, hesap kilitlenmiyor ve test kararsız düşüyordu. Sırasız (bozuk) kodda
/// yanlışlar sıraya girmez, bekleme eskisi gibi onların bitmesine kadar sürer.
/// </para>
/// </summary>
public sealed class GatedPasswordHasher : IPasswordHasher
{
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(3);

    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(50);

    private readonly AspNetPasswordHasher inner = new();
    private readonly string gatedPassword;

    public GatedPasswordHasher(string gatedPassword)
    {
        this.gatedPassword = gatedPassword;
    }

    public ManualResetEventSlim CorrectEntered { get; } = new();

    public ManualResetEventSlim WrongAttemptsDone { get; } = new();

    public bool? WrongAttemptsFinishedWhileCorrectWasVerifying { get; private set; }

    public bool Gate { get; set; }

    /// <summary>Aynı e-postanın giriş sırasında bekleyen istek sayısı (veritabanı kilit tablosundan); verilmezse yalnızca süre.</summary>
    public Func<int>? QueuedAttempts { get; set; }

    public int ExpectedWrongAttempts { get; set; } = 3;

    public string Hash(string password) => this.inner.Hash(password);

    public bool Verify(string? passwordHash, string password)
    {
        if (this.Gate && password == this.gatedPassword)
        {
            this.CorrectEntered.Set();
            Stopwatch watch = Stopwatch.StartNew();
            while (!this.WrongAttemptsDone.Wait(Poll)
                && watch.Elapsed < Wait
                && (this.QueuedAttempts?.Invoke() ?? 0) < this.ExpectedWrongAttempts)
            {
            }

            this.WrongAttemptsFinishedWhileCorrectWasVerifying = this.WrongAttemptsDone.IsSet;
        }

        return this.inner.Verify(passwordHash, password);
    }
}
