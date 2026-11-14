using Konsept.Modules.Identity.Application;
using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Base;
using Konsept.Modules.Identity.Application.Concrete;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Abstract;
using Konsept.Shared.Kernel.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Konsept.Modules.Identity.Tests;

/// <summary>
/// Kayıt, giriş ve oturum servisleri; depolar sahte (NSubstitute), kurallar gerçek (spec 0002 AC-2, AC-5, AC-7, AC-8, AC-9,
/// AC-14, AC-15 servis düzeyi).
/// </summary>
public sealed class AuthServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 9, 0, 0, TimeSpan.Zero));
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ISessionRepository _sessions = Substitute.For<ISessionRepository>();
    private readonly IPasswordHashing _passwords = Substitute.For<IPasswordHashing>();
    private readonly ITokenIssuer _tokens = Substitute.For<ITokenIssuer>();
    private readonly ISystemEventWriter _events = Substitute.For<ISystemEventWriter>();
    private readonly List<SystemEventEntry> _written = [];
    private readonly User _user;

    /// <summary>Initializes a new instance of the <see cref="AuthServiceTests"/> class.</summary>
    public AuthServiceTests()
    {
        _user = User.Register("Ayşe", "ayse@x.com", "HASH", _time.GetUtcNow());
        _passwords.Hash(Arg.Any<string>()).Returns(c => "H:" + c.Arg<string>().Length);
        _passwords.Verify("HASH", "dogru123").Returns(true);
        _tokens.Issue(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>())
            .Returns(c => new IssuedAccessToken("jwt", c.ArgAt<DateTimeOffset>(3).AddMinutes(15)));
        _events.WriteAsync(Arg.Do<SystemEventEntry>(_written.Add), Arg.Any<CancellationToken>()).Returns(c => (SystemEventDto)null!);
        _users.FindByNormalizedEmailAsync("AYSE@X.COM", Arg.Any<CancellationToken>()).Returns(_user);
        _users.FindByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);
        _users.RegisterFailedAttemptAsync(_user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(c => _user.ApplyFailedAttempt(c.ArgAt<DateTimeOffset>(1)));
        _users.RegisterSuccessfulLoginAsync(_user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(c => _user.ApplySuccessfulLogin(c.ArgAt<DateTimeOffset>(1)));
    }

    /// <summary>Kayıtlı olmayan e-posta: genel mesaj, kalan hak yok, hiçbir sayaç ve olay yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_UnknownEmail_GenericMessage_NoCounterNoEvent()
    {
        var result = await Login().LoginAsync(new LoginRequest("yok@x.com", "dogru123"), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe(IdentityErrors.InvalidCredentialsCode);
        result.Error.Message.ShouldBe("E-posta ya da şifre hatalı");
        result.Error.Metadata.ShouldBeNull();
        await _users.DidNotReceiveWithAnyArgs().RegisterFailedAttemptAsync(default, default, TestContext.Current.CancellationToken);
        _written.ShouldBeEmpty();
    }

    /// <summary>Yanlış şifre: 2, 1 hak; 3.'de kilit; kilitliyken doğru şifre reddedilir ve kilit uzamaz; süre dolunca giriş.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Lockout_WrongSequence_ThenLockedRejectsCorrect_ThenOpensAfter15Min()
    {
        var service = Login();
        var wrong = new LoginRequest("ayse@x.com", "yanlis123");

        var first = await service.LoginAsync(wrong, TestContext.Current.CancellationToken);
        var second = await service.LoginAsync(wrong, TestContext.Current.CancellationToken);
        var third = await service.LoginAsync(wrong, TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromMinutes(14));
        var lockedCorrect = await service.LoginAsync(new LoginRequest("AYSE@x.com", "dogru123"), TestContext.Current.CancellationToken);
        var lockedWrong = await service.LoginAsync(wrong, TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)));
        var opened = await service.LoginAsync(new LoginRequest("ayse@x.com", "dogru123"), TestContext.Current.CancellationToken);

        first.Error!.Message.ShouldBe("Yanlış girdiniz, 2 hakkınız kaldı");
        first.Error.Metadata![IdentityErrors.RemainingAttemptsKey].ShouldBe(2);
        second.Error!.Message.ShouldBe("Yanlış girdiniz, 1 hakkınız kaldı");
        var lockedUntil = (DateTimeOffset)third.Error!.Metadata![IdentityErrors.LockedUntilKey];
        third.Error.Code.ShouldBe(IdentityErrors.LockedCode);
        lockedUntil.ShouldBe(new DateTimeOffset(2026, 11, 14, 9, 15, 0, TimeSpan.Zero));
        lockedCorrect.Error!.Code.ShouldBe(IdentityErrors.LockedCode);
        lockedWrong.Error!.Metadata![IdentityErrors.LockedUntilKey].ShouldBe(lockedUntil);
        opened.IsSuccess.ShouldBeTrue();
        _written.Select(e => e.Type).ShouldContain(IdentityEvents.Locked);
        _written.ShouldAllBe(e => e.UserId == _user.Id);
    }

    /// <summary>
    /// Doğru şifre ama başarı kaydı eşzamanlı kilit nedeniyle uygulanmadı: kilitli cevabı ve (kilitli hesap yolundaki gibi)
    /// <c>identity.login_failed</c> uyarısı yazılır, başarı olayı ve oturum yok (review R6-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_CorrectPassword_LockedConcurrently_WritesLoginFailed()
    {
        _users.RegisterSuccessfulLoginAsync(_user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Login().LoginAsync(new LoginRequest("ayse@x.com", "dogru123"), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe(IdentityErrors.LockedCode);
        var written = _written.ShouldHaveSingleItem();
        written.Type.ShouldBe(IdentityEvents.LoginFailed);
        written.Level.ShouldBe(SystemEventLevel.Warning);
        written.UserId.ShouldBe(_user.Id);
        await _sessions.DidNotReceiveWithAnyArgs().AddAsync(default!, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Oturum kaydı başarısız olursa Günlük'e yanlış başarı olayı düşmez (review R10-1): olay oturum açıldıktan sonra yazılır
    /// (giriş ve kayıt).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SessionInsertFails_NoSuccessEvent_LoginAndRegister()
    {
        _sessions.AddAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("db")));
        _users.TryAddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(true);

        await Should.ThrowAsync<InvalidOperationException>(() => Login().LoginAsync(new LoginRequest("ayse@x.com", "dogru123"), TestContext.Current.CancellationToken));
        await Should.ThrowAsync<InvalidOperationException>(() =>
            new RegistrationService(Dependencies()).RegisterAsync(new RegisterRequest("Irem", "irem@x.com", "sifre123"), TestContext.Current.CancellationToken));

        _written.ShouldNotContain(e => e.Type == IdentityEvents.LoginSucceeded);
        _written.ShouldNotContain(e => e.Type == IdentityEvents.Registered);
    }

    /// <summary>2 yanlıştan sonra başarılı giriş sayacı sıfırlar: sonraki yanlışta yine "2 hak".</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Lockout_SuccessAfterTwoFailures_ResetsCounter()
    {
        var service = Login();
        var wrong = new LoginRequest("ayse@x.com", "yanlis123");
        await service.LoginAsync(wrong, TestContext.Current.CancellationToken);
        await service.LoginAsync(wrong, TestContext.Current.CancellationToken);

        (await service.LoginAsync(new LoginRequest("ayse@x.com", "dogru123"), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var next = await service.LoginAsync(wrong, TestContext.Current.CancellationToken);

        next.Error!.Metadata![IdentityErrors.RemainingAttemptsKey].ShouldBe(2);
    }

    /// <summary>Kayıt: e-posta başka harf biçimiyle kayıtlıysa reddedilir, hesap eklenmez; başarıda olay ve oturum.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Register_DuplicateOtherCase_Conflict_NewEmail_SessionAndEvent()
    {
        _users.TryAddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(true);
        var service = new RegistrationService(Dependencies());

        var duplicate = await service.RegisterAsync(new RegisterRequest("Başka", "AYSE@X.COM", "sifre123"), TestContext.Current.CancellationToken);
        var created = await service.RegisterAsync(new RegisterRequest(" Irem ", "irem@x.com", "sifre123"), TestContext.Current.CancellationToken);

        duplicate.Error!.Code.ShouldBe(IdentityErrors.EmailTakenCode);
        duplicate.Error.Message.ShouldBe("Bu e-posta zaten kayıtlı.");
        await _users.Received(1).TryAddAsync(Arg.Is<User>(u => u.Name == "Irem" && u.PasswordHash != "sifre123"), Arg.Any<CancellationToken>());
        created.Value.RefreshToken.ShouldStartWith(SecretMasker.RefreshTokenPrefix);
        created.Value.Name.ShouldBe("Irem");
        await _sessions.Received(1).AddAsync(Arg.Is<UserSession>(s => s.RefreshTokenHash != created.Value.RefreshToken), Arg.Any<CancellationToken>());
        _written.Single().Type.ShouldBe(IdentityEvents.Registered);
    }

    /// <summary>Çıkış geçerli kullanıcının tüm oturumlarını iptal eder ve olay yazar; doğrulama iptal edilen oturumu reddeder.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Logout_RevokesAllSessions_ValidateRejectsRevoked()
    {
        var current = Substitute.For<ICurrentUser>();
        current.UserId.Returns(_user.Id);
        var session = UserSession.Open(_user.Id, "h", _time.GetUtcNow());
        _sessions.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _sessions.RevokeAllAsync(_user.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(c => { session.Revoke(c.ArgAt<DateTimeOffset>(1)); return 1; });
        var service = new SessionService(Dependencies(), current, NullLogger<SessionService>.Instance);

        (await service.ValidateAsync(session.Id, TestContext.Current.CancellationToken)).ShouldBeTrue();
        var logout = await service.LogoutAsync(TestContext.Current.CancellationToken);

        logout.Value.ShouldBe(1);
        (await service.ValidateAsync(session.Id, TestContext.Current.CancellationToken)).ShouldBeFalse();
        _written.Single().Type.ShouldBe(IdentityEvents.LoggedOut);
        _written.Single().UserId.ShouldBe(_user.Id);
    }

    /// <summary>Refresh token 'knr_' önekli ve her seferinde farklıdır; hash düz metin değildir.</summary>
    [Fact]
    public void RefreshTokenFactory_RandomPrefixed_HashIsNotPlain()
    {
        var factory = new RefreshTokenFactory();

        var a = factory.Create();
        var b = factory.Create();

        a.ShouldStartWith("knr_");
        a.ShouldNotBe(b);
        factory.Hash(a).ShouldNotContain(a[4..]);
        factory.Hash(a).Length.ShouldBe(64);
        SecretMasker.Mask($"token {a}").ShouldBe("token ***");
    }

    private LoginService Login() => new(Dependencies());

    private AuthServiceDependencies Dependencies() =>
        new(_users, _sessions, _passwords, _tokens, new RefreshTokenFactory(), _events, _time, NullLogger<AuthServiceBase>.Instance);
}
