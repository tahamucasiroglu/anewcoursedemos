using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Commands.PlatformAdmin;
using OkulYonetim.Persistence;

namespace OkulYonetim.WebAPI.Cli;

/// <summary>
/// Platform yöneticisi komutları (spec 0007): <c>grant-platform-admin --eposta …</c>, <c>revoke-platform-admin --eposta …</c>,
/// <c>reset-two-factor --eposta …</c>. Web'den yapılamaz. Kişinin açık oturumları kapanır; audit'e komut kaynağıyla yazılır.
/// Çıkış kodu: 0 başarılı, 1 kişi yok, 2 kullanım hatası.
/// </summary>
public static class PlatformAdminCli
{
    public static readonly IReadOnlyDictionary<string, PlatformAdminAction> Commands = new Dictionary<string, PlatformAdminAction>(StringComparer.Ordinal)
    {
        ["grant-platform-admin"] = PlatformAdminAction.Grant,
        ["revoke-platform-admin"] = PlatformAdminAction.Revoke,
        ["reset-two-factor"] = PlatformAdminAction.ResetTwoFactor,
    };

    public static async Task<int> RunAsync(IServiceProvider services, string command, IReadOnlyList<string> args, TextWriter output, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);
        if (!Commands.TryGetValue(command, out PlatformAdminAction action) || args.Count != 2 || args[0] != "--eposta" || string.IsNullOrWhiteSpace(args[1]))
        {
            await output.WriteLineAsync($"Kullanım: {command} --eposta \"kisi@ornek.com\"").ConfigureAwait(false);
            return 2;
        }

        services.MigrateOkulDatabase();
        using IServiceScope scope = services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        bool done = await sender.Send(new PlatformAdminCommand(args[1], action, $"komut satırı: {Environment.UserName}@{Environment.MachineName}"), cancellationToken).ConfigureAwait(false);
        if (!done)
        {
            await output.WriteLineAsync("Bu e-postayla bir hesap yok; hiçbir şey değişmedi.").ConfigureAwait(false);
            return 1;
        }

        await output.WriteLineAsync(action switch
        {
            PlatformAdminAction.Grant => "Platform yöneticisi yetkisi verildi; ilk girişte iki adımlı doğrulama kurulacak. Açık oturumlar kapatıldı.",
            PlatformAdminAction.Revoke => "Platform yöneticisi yetkisi alındı. Açık oturumlar kapatıldı.",
            _ => "İki adımlı doğrulama sıfırlandı; sonraki girişte yeniden kurulacak. Açık oturumlar kapatıldı.",
        }).ConfigureAwait(false);
        return 0;
    }
}
