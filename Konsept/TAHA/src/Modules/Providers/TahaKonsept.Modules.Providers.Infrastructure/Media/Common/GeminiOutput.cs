using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Çözülmüş Gemini <c>generateContent</c> yanıtı: <c>inlineData</c> parçaları ve <c>usageMetadata</c>'dan token kullanımı.
/// </summary>
/// <param name="Parts">Satır içi veri parçaları (görsel ya da ses).</param>
/// <param name="TokenUsage">Giriş/çıkış token'ları.</param>
public sealed record GeminiOutput(IReadOnlyList<GeminiInlineData> Parts, CallUsage TokenUsage);
