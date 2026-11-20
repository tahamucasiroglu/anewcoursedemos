namespace OkulYonetim.Application.Ports;

/// <summary>
/// İstek sahibi girişli ama bu işleme yetkisi yok (403; spec 0005). nArchitecture'ın <c>AuthorizationException</c>'ı 401
/// döner ve istemciyi oturum yenilemeye götürür; yetki eksikliği oturum sorunu değildir.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException()
    {
    }

    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
