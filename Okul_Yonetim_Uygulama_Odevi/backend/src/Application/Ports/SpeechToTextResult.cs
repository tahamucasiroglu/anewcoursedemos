namespace OkulYonetim.Application.Ports;

/// <summary>Sesin dökümü (boş olabilir — anlaşılmayan kayıt) ve ses servisinin ölçtüğü süre (saniye).</summary>
public sealed record SpeechToTextResult(string Text, double DurationSeconds);
