namespace TahaKonsept.Modules.Concepts.Domain.Constants;

/// <summary>
/// Concepts modülünün sınırları ve varsayılanları (kolon uzunlukları, doğrulama, istek adetleri, konu hafızası tavanı)
/// tek yerden buradan okunur.
/// </summary>
public static class ConceptLimits
{
    /// <summary>Başlık en fazla uzunluk.</summary>
    public const int TitleMaxLength = 300;

    /// <summary>Model anahtarı en fazla uzunluk.</summary>
    public const int ModelKeyMaxLength = 160;

    /// <summary>Bir istekte en az adet.</summary>
    public const int MinCount = 1;

    /// <summary>Bir istekte en fazla adet (konu önerisi / fikirden üretim / seçilen başlık).</summary>
    public const int MaxCount = 100;

    /// <summary>Fikir metni en fazla uzunluk.</summary>
    public const int IdeaMaxLength = 4000;

    /// <summary>Fikir metni en az uzunluk.</summary>
    public const int IdeaMinLength = 5;

    /// <summary>Konu önerisi yönü en fazla uzunluk.</summary>
    public const int HintMaxLength = 300;

    /// <summary>Düzeltme talimatı en fazla uzunluk.</summary>
    public const int InstructionMaxLength = 4000;

    /// <summary>Ton / izleyici / anlatım alanları en fazla uzunluk.</summary>
    public const int StyleMaxLength = 200;

    /// <summary>Serbest ince ayar notu en fazla uzunluk.</summary>
    public const int NotesMaxLength = 1500;

    /// <summary>Konu hafızası anahtar kelime alanı en fazla uzunluk.</summary>
    public const int KeywordsMaxLength = 400;

    /// <summary>Belgedeki en fazla sahne sayısı.</summary>
    public const int MaxScenes = 60;

    /// <summary>Sahne metin alanları en fazla uzunluk.</summary>
    public const int SceneTextMaxLength = 4000;

    /// <summary>Varsayılan konsept modeli.</summary>
    public const string DefaultConceptModelKey = "claude-sonnet-5-5";

    /// <summary>Varsayılan konu önerisi modeli.</summary>
    public const string DefaultTopicModelKey = "claude-haiku-5-5";

    /// <summary>Varsayılan kelime denetimi modeli.</summary>
    public const string DefaultWordCheckModelKey = "claude-haiku-5-5";

    /// <summary>
    /// Türkçe seslendirme hızı yedeği (kelime / saniye) — üretim tarafı seçili ses modelinin hızını vermezse anlatım uzunluğu
    /// bu değerle boyutlandırılır. (Eski 2,4 değeri Supertonic 3'te videoları ~%40 uzun çıkarıyordu; ölçüm ~1,7.)
    /// </summary>
    public const double TurkishWordsPerSecond = 2.0;

    /// <summary>Diğer diller için varsayılan seslendirme hızı (kelime / saniye).</summary>
    public const double DefaultWordsPerSecond = 2.6;
}
