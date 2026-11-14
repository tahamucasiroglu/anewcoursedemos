using Shouldly;
using TahaKonsept.Agent.Engines.Video.Safety.Concrete;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Unit;

/// <summary>Sahne kodu güvenlik denetimi: ağ, file://, deterministik olmayan API'ler; SVG ad alanı serbest.</summary>
public sealed class SceneCodeGuardTests
{
    private readonly DefaultSceneCodeGuard _guard = new();

    /// <summary>Yasak kalıplar reddedilir.</summary>
    [Theory]
    [InlineData("fetch('data.json')")]
    [InlineData("<script src=\"https://cdn.jsdelivr.net/npm/gsap@3/dist/gsap.min.js\"></script>")]
    [InlineData("new XMLHttpRequest()")]
    [InlineData("const x = Math.random();")]
    [InlineData("<img src=\"file:///C:/a.png\">")]
    [InlineData("@import url('https://fonts.googleapis.com/css2?family=Inter');")]
    [InlineData("const t = Date.now();")]
    public void Unsafe_IsRejected(string code) => _guard.Check(code).IsFailure.ShouldBeTrue();

    /// <summary>Temiz kod ve SVG ad alanı kabul edilir.</summary>
    [Fact]
    public void Clean_IsAccepted() =>
        _guard.Check("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg><script src=\"gsap.min.js\"></script><script>gsap.timeline({paused:true}); // yorum</script>")
            .IsSuccess.ShouldBeTrue();

    /// <summary>Boş kod reddedilir.</summary>
    [Fact]
    public void Empty_IsRejected() => _guard.Check("  ").Error.Code.ShouldBe("scene.code_empty");
}
