using OkulYonetim.WebAPI.Cli;

namespace OkulYonetim.WebAPI;

/// <summary>
/// Web API giriş noktası: uygulamayı <see cref="OkulApi"/> ile kurar ve çalıştırır. İlk argüman <c>create-school</c> ise
/// sunucu açılmaz, okul açma komutu (<see cref="CreateSchoolCli"/>) aynı ayarlarla çalışır; platform yöneticisi
/// komutları (<see cref="PlatformAdminCli"/>) da öyle (spec 0007).
/// </summary>
public sealed class Program
{
    private Program()
    {
    }

    public static async Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length > 0 && args[0] == CreateSchoolCli.CommandName)
        {
            string[] commandArgs = args[1..];
            await using WebApplication cli = OkulApi.Build([]);
            return await CreateSchoolCli.RunAsync(cli.Services, commandArgs, Console.Out, CreateSchoolCli.ReadSecretFromConsole, CancellationToken.None).ConfigureAwait(false);
        }

        if (args.Length > 0 && PlatformAdminCli.Commands.ContainsKey(args[0]))
        {
            await using WebApplication cli = OkulApi.Build([]);
            return await PlatformAdminCli.RunAsync(cli.Services, args[0], args[1..], Console.Out, CancellationToken.None).ConfigureAwait(false);
        }

        await OkulApi.Build(args).RunAsync().ConfigureAwait(false);
        return 0;
    }
}
