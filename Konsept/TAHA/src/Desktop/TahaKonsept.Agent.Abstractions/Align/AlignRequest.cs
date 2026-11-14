namespace TahaKonsept.Agent.Abstractions.Align;

/// <summary>Hizalama isteği.</summary>
/// <param name="AudioPath">Ses dosyası (tamamı).</param>
/// <param name="Segments">Sahne/cümle segmentleri, sıralı.</param>
/// <param name="Language">Dil kodu (ör. <c>tr</c>).</param>
public sealed record AlignRequest(string AudioPath, IReadOnlyList<CaptionSegment> Segments, string Language);
