using System.Text.Json.Serialization;

namespace TahaKonsept.Web.Models;

/// <summary>Başlangıç kontrol listesindeki bir adımın durumu (JSON'da metin: <c>Done</c> / <c>Waiting</c> / <c>Unknown</c>).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<OnboardingStepState>))]
public enum OnboardingStepState
{
    /// <summary>Adım henüz yapılmadı.</summary>
    Waiting = 0,

    /// <summary>Adım tamam.</summary>
    Done = 1,

    /// <summary>Durum öğrenilemedi (API yanıt vermedi).</summary>
    Unknown = 2,
}
