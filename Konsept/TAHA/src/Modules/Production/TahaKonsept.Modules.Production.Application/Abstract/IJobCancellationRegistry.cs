namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Çalışan sıra öğelerinin (işlerin) iptal belirteçleri (singleton). Orkestratör bir öğenin işini ilerletirken <see cref="Begin"/> ile öğeye bağlı
/// belirteç alır (sunucu aşamaları ve sürmekte olan LLM/TTS/görsel çağrıları bu belirteçle çalışır); sıradan çıkarma isteği <see cref="Cancel"/> ile
/// kullanıcı kilidini beklemeden zinciri anında durdurur, ardından kilidi alıp işi iptal eder. Anahtar kullanıcı + öğe (başka kullanıcının işi iptal edilemez).
/// </summary>
public interface IJobCancellationRegistry
{
    /// <summary>Öğe için yeni iptal kapsamı açar (dispose edilince kayıt silinir).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="queueItemId">Sıra öğesi.</param>
    /// <param name="cancellationToken">Bağlanacak üst belirteç (ör. uygulama durdurma).</param>
    /// <returns>Kapsam; <see cref="IJobCancellationScope.Token"/> hem üst belirteçle hem iptal isteğiyle iptal olur.</returns>
    IJobCancellationScope Begin(Guid userId, Guid queueItemId, CancellationToken cancellationToken);

    /// <summary>Öğenin çalışan zincirini iptal eder (kapsam yoksa etkisiz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="queueItemId">Sıra öğesi.</param>
    /// <returns>Çalışan bir kapsam iptal edildiyse <c>true</c>.</returns>
    bool Cancel(Guid userId, Guid queueItemId);
}
