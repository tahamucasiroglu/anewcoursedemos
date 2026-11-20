using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Security;

namespace OkulYonetim.Tests.Integration.Identity;

/// <summary>
/// Eşzamanlılık testi için gerçek şifre doğrulayıcının sarmalayıcısı (review H-1): doğru şifrenin doğrulaması,
/// <see cref="WrongAttemptsDone"/> işaretlenene kadar (en fazla <see cref="Wait"/>) bekletilir. Böylece "doğru şifre
/// kilit kontrolünü geçtikten sonra yanlış denemeler hesabı kilitler" sırası zorlanır. Bekleme bitince işaretin o anki
/// durumu <see cref="WrongAttemptsFinishedWhileCorrectWasVerifying"/>'e yazılır.
/// </summary>
public sealed class GatedPasswordHasher : IPasswordHasher
{
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(3);

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

    public string Hash(string password) => this.inner.Hash(password);

    public bool Verify(string? passwordHash, string password)
    {
        if (this.Gate && password == this.gatedPassword)
        {
            this.CorrectEntered.Set();
            this.WrongAttemptsFinishedWhileCorrectWasVerifying = this.WrongAttemptsDone.Wait(Wait);
        }

        return this.inner.Verify(passwordHash, password);
    }
}
