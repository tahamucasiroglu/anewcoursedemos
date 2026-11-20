using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Commands.CreateSchool;
using OkulYonetim.Application.Features.Identity.Queries.UserExists;
using OkulYonetim.Persistence;

namespace OkulYonetim.WebAPI.Cli;

/// <summary>
/// Okul açma komutu (spec 0004, gereksinim 1): <c>OkulYonetim.WebAPI create-school --okul "…" --sube "…" --ad "…"
/// --eposta "…"</c>. Şifre argüman olarak verilmez: kişi yeniyse gizli girişle iki kez sorulur (ekranda, komut
/// satırında ve logda görünmez); kişi varsa sorulmaz ve şifresi değişmez. Önce şema geçişleri uygulanır. Çıkış kodu:
/// 0 başarılı / zaten var, 1 hata, 2 kullanım hatası.
/// </summary>
public static class CreateSchoolCli
{
    public const string CommandName = "create-school";

    public static async Task<int> RunAsync(
        IServiceProvider services,
        IReadOnlyList<string> args,
        TextWriter output,
        Func<string?> readSecret,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(readSecret);
        Dictionary<string, string> options = ParseOptions(args);
        if (!options.TryGetValue("okul", out string? school) || !options.TryGetValue("sube", out string? branch)
            || !options.TryGetValue("ad", out string? fullName) || !options.TryGetValue("eposta", out string? email))
        {
            await output.WriteLineAsync("Kullanım: create-school --okul \"Okul adı\" --sube \"İlk şube adı\" --ad \"Kurucu ad soyad\" --eposta \"kurucu@ornek.com\"").ConfigureAwait(false);
            return 2;
        }

        services.MigrateOkulDatabase();
        using IServiceScope scope = services.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        try
        {
            string? password = null;
            if (!await sender.Send(new UserExistsQuery(email), cancellationToken).ConfigureAwait(false))
            {
                await output.WriteAsync("Kurucu şifresi (en az 8 karakter, ekranda görünmez): ").ConfigureAwait(false);
                password = readSecret();
                await output.WriteAsync("Şifre (tekrar): ").ConfigureAwait(false);
                if (!string.Equals(password, readSecret(), StringComparison.Ordinal))
                {
                    await output.WriteLineAsync("HATA: şifreler aynı değil; hiçbir şey oluşturulmadı.").ConfigureAwait(false);
                    return 1;
                }
            }

            CreateSchoolResult result = await sender.Send(new CreateSchoolCommand(school, branch, fullName, email, password, $"komut satırı: {Environment.UserName}@{Environment.MachineName}"), cancellationToken).ConfigureAwait(false);
            await output.WriteLineAsync(result.AlreadyExisted
                ? $"Zaten var: bu kişi \"{school.Trim()}\" okulunun kurucusu; hiçbir şey değiştirilmedi."
                : $"Okul açıldı: {school.Trim()} · ilk şube: {branch.Trim()} · kurucu: {email.Trim()} ({(result.UserCreated ? "yeni hesap" : "mevcut hesap, şifresi değişmedi")})").ConfigureAwait(false);
            return 0;
        }
        catch (ValidationException exception)
        {
            foreach (ValidationExceptionModel error in exception.Errors)
            {
                foreach (string message in error.Errors ?? [])
                {
                    await output.WriteLineAsync($"HATA: {message}").ConfigureAwait(false);
                }
            }

            await output.WriteLineAsync("Hiçbir şey oluşturulmadı.").ConfigureAwait(false);
            return 1;
        }
        catch (BusinessException exception)
        {
            await output.WriteLineAsync($"HATA: {exception.Message}; hiçbir şey oluşturulmadı.").ConfigureAwait(false);
            return 1;
        }
    }

    /// <summary>Konsoldan gizli okuma: yönlendirilmiş girişte bir satır, terminalde tuşlar ekrana yazılmadan.</summary>
    public static string? ReadSecretFromConsole()
    {
        if (Console.IsInputRedirected)
        {
            return Console.In.ReadLine();
        }

        System.Text.StringBuilder secret = new();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return secret.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (secret.Length > 0)
                {
                    secret.Length--;
                }

                continue;
            }

            secret.Append(key.KeyChar);
        }
    }

    private static Dictionary<string, string> ParseOptions(IReadOnlyList<string> args)
    {
        Dictionary<string, string> options = new(StringComparer.Ordinal);
        for (int i = 0; i + 1 < args.Count; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                options[args[i][2..]] = args[i + 1];
                i++;
            }
        }

        return options;
    }
}
