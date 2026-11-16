namespace Konsept.Web.ApiClient.Dtos;

/// <summary>Konsept düzenleme komutları (spec 0015, 0016); her biri tek bir Api ucuna karşılık gelir.</summary>
public enum ConceptEditCommand
{
    /// <summary>Sahneleri kaydet (<c>POST api/concepts/{id}/scenes</c>).</summary>
    SaveScenes,

    /// <summary>Yeniden üret / Tekrar düzenle (<c>POST api/concepts/{id}/regenerate</c>).</summary>
    Regenerate,

    /// <summary>Revizyonu onayla (<c>POST api/concepts/{id}/revision/approve</c>).</summary>
    ApproveRevision,

    /// <summary>Revizyonu sil (<c>POST api/concepts/{id}/revision/discard</c>).</summary>
    DiscardRevision,

    /// <summary>Silinenler'e taşı (<c>POST api/concepts/{id}/delete</c>; spec 0016).</summary>
    Delete,

    /// <summary>Kalıcı sil (<c>POST api/concepts/deleted/{id}/purge</c>; spec 0016).</summary>
    Purge,
}
