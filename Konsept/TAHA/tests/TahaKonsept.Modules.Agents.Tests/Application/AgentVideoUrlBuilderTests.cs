using Shouldly;
using TahaKonsept.Modules.Agents.Application.Concrete;
using Xunit;

namespace TahaKonsept.Modules.Agents.Tests.Application;

/// <summary>
/// <see cref="AgentVideoUrlBuilder"/>: video/küçük resim/dosya adresleri, IPv6 köşeli parantez, anahtar URL-kodlama.
/// </summary>
public sealed class AgentVideoUrlBuilderTests
{
    private static readonly Guid VideoId = Guid.Parse("0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b");
    private readonly AgentVideoUrlBuilder _builder = new();

    /// <summary>IPv4 video adresi.</summary>
    [Fact]
    public void Builds_video_url()
    {
        _builder.BuildVideoUrl("192.168.1.20", 7120, "abc123", VideoId)
            .ShouldBe("http://192.168.1.20:7120/videos/0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b?k=abc123");
    }

    /// <summary>Küçük resim adresi.</summary>
    [Fact]
    public void Builds_thumbnail_url()
    {
        _builder.BuildThumbnailUrl("192.168.1.20", 7120, "abc123", VideoId)
            .ShouldBe("http://192.168.1.20:7120/thumbs/0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b?k=abc123");
    }

    /// <summary>IPv6 adres köşeli paranteze alınır, anahtar kodlanır.</summary>
    [Fact]
    public void Brackets_ipv6_and_escapes_key()
    {
        _builder.BuildVideoUrl("fe80::1", 7120, "a+b/c=", VideoId)
            .ShouldBe("http://[fe80::1]:7120/videos/0193a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b?k=a%2Bb%2Fc%3D");
    }

    /// <summary>Göreli dosya yolu; sorgu varsa <c>&amp;</c> ile eklenir.</summary>
    [Fact]
    public void Builds_file_url_with_existing_query()
    {
        _builder.BuildFileUrl("10.0.0.5", 8000, "key", "previews/x.wav").ShouldBe("http://10.0.0.5:8000/previews/x.wav?k=key");
        _builder.BuildFileUrl("10.0.0.5", 8000, "key", "/a?b=1").ShouldBe("http://10.0.0.5:8000/a?b=1&k=key");
    }

    /// <summary>Geçersiz port reddedilir.</summary>
    [Fact]
    public void Rejects_invalid_port()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => _builder.BuildVideoUrl("10.0.0.5", 0, "k", VideoId));
    }
}
