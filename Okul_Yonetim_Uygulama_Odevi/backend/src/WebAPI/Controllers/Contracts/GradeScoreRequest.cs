using System.Text.Json;
using OkulYonetim.Application.Features.Academics.Assessments;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Bir öğrencinin puanı (spec 0013 AC-6): JSON sayısı, metin ya da <c>null</c> (girilmedi). Değer olduğu gibi (ham metniyle)
/// uygulamaya geçer ve orada doğrulanır — ondalık (<c>85.5</c>) ya da sayı olmayan değer (<c>"abc"</c>) çerçevenin İngilizce
/// bağlama hatasına değil, öğrenciyi belirten Türkçe alan hatasına düşer.
/// </summary>
public sealed record GradeScoreRequest(Guid StudentId, JsonElement? Score)
{
    public GradeInput ToInput() => new(this.StudentId, this.Score switch
    {
        null => null,
        { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        { ValueKind: JsonValueKind.String } value => value.GetString(),
        { } value => value.GetRawText(),
    });
}
