namespace TahaKonsept.Agent.Core.Videos.Abstract;

/// <summary>Video kimliği → yerel dosya eşlemesini kalıcı (JSON) tutan dizin. Video sunucusu ve kütüphane kullanır.</summary>
public interface IVideoIndex
{
    /// <summary>Kaydı bulur.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    VideoEntry? Find(Guid videoId);

    /// <summary>Tüm kayıtlar (yeniden eskiye).</summary>
    /// <returns>Kayıtlar.</returns>
    IReadOnlyList<VideoEntry> GetAll();

    /// <summary>Kaydı ekler ya da günceller.</summary>
    /// <param name="entry">Kayıt.</param>
    void Upsert(VideoEntry entry);

    /// <summary>Kaydı siler.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Silindiyse <c>true</c>.</returns>
    bool Remove(Guid videoId);
}
