namespace Konsept.Shared.Kernel.Security;

/// <summary>
/// Web'in giriş/kayıt çağrılarında tarayıcının IP adresini Api'ye ilettiği başlık (plan 0002, Risk 5). Api bu başlığa
/// yalnızca geçerli servis anahtarıyla (<see cref="ServiceKeyHeader"/>) birlikte geldiğinde güvenir; aksi halde
/// bağlantının kendi adresini kullanır.
/// </summary>
public static class ClientIpHeader
{
    /// <summary>Başlık adı.</summary>
    public const string Name = "X-Konsept-Client-Ip";
}
