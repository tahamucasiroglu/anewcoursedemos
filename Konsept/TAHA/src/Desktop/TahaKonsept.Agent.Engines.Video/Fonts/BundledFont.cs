namespace TahaKonsept.Agent.Engines.Video.Fonts;

/// <summary>Motor şablonuyla birlikte gelen (fontsource npm paketi) yerel yazı tipi.</summary>
/// <param name="Family">CSS font-family adı (ör. <c>Inter</c>).</param>
/// <param name="PackageName">npm paketi (ör. <c>@fontsource/inter</c>).</param>
/// <param name="FilePrefix">Dosya adı öneki (ör. <c>inter</c>).</param>
/// <param name="Weights">Kopyalanan ağırlıklar.</param>
public sealed record BundledFont(string Family, string PackageName, string FilePrefix, IReadOnlyList<int> Weights);
