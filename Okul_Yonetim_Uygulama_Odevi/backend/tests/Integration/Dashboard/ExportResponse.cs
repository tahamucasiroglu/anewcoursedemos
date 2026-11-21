using System.Net;

namespace OkulYonetim.Tests.Integration.Dashboard;

/// <summary>Dışa aktarma yanıtı (spec 0017 testleri): durum, gövde baytları, içerik türü, ek dosya adı ve gövdenin UTF-8 metni.</summary>
public sealed record ExportResponse(HttpStatusCode Status, byte[] Bytes, string? ContentType, string? FileName, string Text);
