namespace OkulYonetim.WebAPI;

/// <summary>Web API giriş noktası: uygulamayı <see cref="OkulApi"/> ile kurar ve çalıştırır.</summary>
public sealed class Program
{
    private Program()
    {
    }

    public static void Main(string[] args)
    {
        OkulApi.Build(args).Run();
    }
}
