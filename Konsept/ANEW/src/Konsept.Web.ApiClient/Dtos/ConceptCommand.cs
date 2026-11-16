namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Konseptler sayfasının Api'ye ilettiği komutlar (spec 0014); her biri tek bir Api ucuna karşılık gelir.</summary>
public enum ConceptCommand
{
    /// <summary>Konu üret (<c>POST api/concepts/topics</c>).</summary>
    Topics,

    /// <summary>Seçtiklerini üret (<c>POST api/concepts/from-topics</c>).</summary>
    FromTopics,

    /// <summary>Fikirden üret (<c>POST api/concepts/from-idea</c>).</summary>
    FromIdea,

    /// <summary>Durdur (<c>POST api/concepts/jobs/stop</c>).</summary>
    Stop,

    /// <summary>Onayla (<c>POST api/concepts/approve</c>).</summary>
    Approve,

    /// <summary>Onayı geri al (<c>POST api/concepts/unapprove</c>).</summary>
    Unapprove,

    /// <summary>Önerileri sıfırla (<c>POST api/concepts/suggestions/reset</c>; spec 0016).</summary>
    ResetSuggestions,
}
