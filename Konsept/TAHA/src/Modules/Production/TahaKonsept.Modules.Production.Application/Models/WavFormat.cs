namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// WAV <c>fmt </c> parçasının özü: biçim etiketi (1 = PCM, 3 = float), kanal, örnekleme hızı, örnek bit derinliği ve blok hizası.
/// </summary>
/// <param name="FormatTag">Biçim etiketi.</param>
/// <param name="Channels">Kanal sayısı.</param>
/// <param name="SampleRate">Örnekleme hızı (Hz).</param>
/// <param name="BitsPerSample">Örnek başına bit.</param>
/// <param name="BlockAlign">Kare başına bayt.</param>
public readonly record struct WavFormat(ushort FormatTag, ushort Channels, int SampleRate, ushort BitsPerSample, ushort BlockAlign);
