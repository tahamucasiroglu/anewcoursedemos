namespace TahaKonsept.Agent.Abstractions.Align;

/// <summary>Bir metin segmenti: ekranda görünen yazılış, sese giden okunuş ve (biliniyorsa) ses içindeki zaman aralığı.</summary>
/// <param name="DisplayText">Yazılış (altyazı).</param>
/// <param name="SpokenText">Okunuş (TTS).</param>
/// <param name="StartSec">Başlangıç (sn) ya da bilinmiyorsa <c>null</c>.</param>
/// <param name="EndSec">Bitiş (sn) ya da bilinmiyorsa <c>null</c>.</param>
public sealed record CaptionSegment(string DisplayText, string SpokenText, double? StartSec, double? EndSec);
