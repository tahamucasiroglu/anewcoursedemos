using System.Text.RegularExpressions;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Pi kurulumu ilk admin ayarına dokunmaz (spec 0011 Req 5, plan Risk 8): <c>Identity__AdminEmail</c> satırı
/// <c>/etc/anewconcept/api.env</c>'e elle/orkestratörce yazılır; kurulum betiği bu satırı yazmaz, silmez, ezmez; api.env
/// yalnızca yoksa oluşturulur. Repoda (appsettings, deploy) gerçek bir e-posta değeri yoktur.
/// </summary>
public sealed partial class DeployAdminEmailTests
{
    /// <summary>
    /// Betikte <c>Identity__AdminEmail</c> yalnızca yorumlarda geçer; api.env'e yazan (<c>&gt;</c>) her satır ya "yoksa oluştur"
    /// bloğunda ya da "satır yoksa ekle" (<c>&gt;&gt;</c>) biçimindedir; api.env'i silen ya da yerinde düzenleyen komut yoktur.
    /// </summary>
    [Fact]
    public void Provision_NeverWritesRemovesOrOverwritesAdminEmail()
    {
        var lines = File.ReadAllLines(Path.Combine(RepositoryRoot(), "deploy", "pi", "provision.sh"));

        var code = lines.Where(l => !l.TrimStart().StartsWith('#')).ToList();
        code.ShouldNotContain(l => l.Contains("AdminEmail", StringComparison.OrdinalIgnoreCase));
        lines.ShouldContain(l => l.TrimStart().StartsWith('#') && l.Contains("Identity__AdminEmail", StringComparison.Ordinal));

        var overwrites = code.Where(l => OverwriteApiEnv().IsMatch(l)).ToList();
        overwrites.ShouldHaveSingleItem().ShouldContain("ConnectionStrings__Konsept=");
        var createBlock = string.Join('\n', lines);
        createBlock.ShouldContain("if [ ! -f /etc/anewconcept/api.env ]; then");
        createBlock.IndexOf("if [ ! -f /etc/anewconcept/api.env ]; then", StringComparison.Ordinal)
            .ShouldBeLessThan(createBlock.IndexOf(overwrites[0], StringComparison.Ordinal));

        foreach (var append in code.Where(l => l.Contains(">> /etc/anewconcept/api.env", StringComparison.Ordinal)))
        {
            var key = AppendedKey().Match(append).Groups["key"].Value;
            key.ShouldNotBeNullOrEmpty(append);
            code.ShouldContain(l => l.Contains($"if ! grep -q '^{key}=' /etc/anewconcept/api.env", StringComparison.Ordinal), key);
        }

        code.ShouldNotContain(l => l.Contains("sed -i", StringComparison.Ordinal) && l.Contains("api.env", StringComparison.Ordinal));
        code.ShouldNotContain(l => RemovesApiEnv().IsMatch(l));
    }

    /// <summary>Api'nin appsettings'inde ayar anahtarı vardır ama değeri boştur (repoda e-posta yok).</summary>
    [Fact]
    public void Appsettings_AdminEmail_PresentButEmpty()
    {
        var json = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "Konsept.Api", "appsettings.json"));

        json.ShouldContain("\"AdminEmail\": \"\"");
        json.ShouldNotContain("@");
    }

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Konsept.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Konsept.sln bulunamadı.");
    }

    [GeneratedRegex(@"(?<!>)>\s*/etc/anewconcept/api\.env")]
    private static partial Regex OverwriteApiEnv();

    [GeneratedRegex(@"printf '(?<key>[A-Za-z_]+)=")]
    private static partial Regex AppendedKey();

    [GeneratedRegex(@"\b(rm|mv|truncate)\b[^\n]*api\.env")]
    private static partial Regex RemovesApiEnv();
}
