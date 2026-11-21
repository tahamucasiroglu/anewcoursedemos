using System.Text.Json;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// İstekteki sayısal alanın ham metni (spec 0013 <c>GradeScoreRequest</c> kalıbı; spec 0015 tutar ve yüzde): JSON sayısı ham metniyle,
/// metin olduğu gibi, yok/null boş. Doğrulama uygulamada yapılır — "abc", <c>1.005</c> ya da <c>15.5</c> çerçevenin İngilizce bağlama
/// hatasına değil Türkçe alan hatasına düşer; ondalık değer hiçbir aşamada kayan noktalı sayıya çevrilmez.
/// </summary>
public static class JsonValueText
{
    public static string? Of(JsonElement? value) => value switch
    {
        null => null,
        { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        { ValueKind: JsonValueKind.String } text => text.GetString(),
        { } other => other.GetRawText(),
    };
}
