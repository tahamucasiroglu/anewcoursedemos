using System.Security.Cryptography;
using System.Text.Json;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Shared.Contracts.Common;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Agent.Core.Auth.Base;

/// <summary>
/// Token deposu tabanı: JSON'a çevirip <see cref="Protect"/> ile şifreler ve <c>ConfigDir/tokens.bin</c> dosyasına yazar.
/// Şifre çözülemeyen (başka kullanıcı/bozuk) dosya silinir ve <c>null</c> döner. Şifreleme yöntemi türetilen sınıftadır.
/// </summary>
public abstract class TokenStoreBase : ITokenStore
{
    /// <summary>Token dosyasının adı.</summary>
    public const string FileName = "tokens.bin";

    private readonly object _gate = new();

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    protected TokenStoreBase(IAgentPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        FilePath = Path.Combine(paths.ConfigDir, FileName);
    }

    /// <summary>Token dosyasının yolu.</summary>
    protected string FilePath { get; }

    /// <inheritdoc />
    public TokenResponse? Load()
    {
        lock (_gate)
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            try
            {
                var plain = Unprotect(File.ReadAllBytes(FilePath));
                return JsonSerializer.Deserialize<TokenResponse>(plain, ContractJson.Options);
            }
            catch (Exception ex) when (ex is CryptographicException or JsonException)
            {
                TryDelete();
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public void Save(TokenResponse tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var cipher = Protect(JsonSerializer.SerializeToUtf8Bytes(tokens, ContractJson.Options));
            var temp = FilePath + ".tmp";
            File.WriteAllBytes(temp, cipher);
            File.Move(temp, FilePath, overwrite: true);
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_gate)
        {
            TryDelete();
        }
    }

    /// <summary>Düz baytları şifreler.</summary>
    /// <param name="plain">Düz veri.</param>
    /// <returns>Şifreli veri.</returns>
    protected abstract byte[] Protect(byte[] plain);

    /// <summary>Şifreli baytları çözer.</summary>
    /// <param name="cipher">Şifreli veri.</param>
    /// <returns>Düz veri.</returns>
    protected abstract byte[] Unprotect(byte[] cipher);

    private void TryDelete()
    {
        try
        {
            File.Delete(FilePath);
        }
        catch (IOException)
        {
            // Silinemedi; bir sonraki kayıtta üzerine yazılır.
        }
    }
}
