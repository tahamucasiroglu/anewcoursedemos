namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Gemini <c>generateContent</c> yanıtındaki bir <c>inlineData</c> parçası (çözülmüş base64 içerik + MIME türü).
/// </summary>
/// <param name="Data">İçerik baytları.</param>
/// <param name="MimeType">MIME türü (ör. <c>image/png</c>, <c>audio/L16;codec=pcm;rate=24000</c>).</param>
public sealed record GeminiInlineData(byte[] Data, string MimeType);
