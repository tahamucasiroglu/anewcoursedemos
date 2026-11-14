using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Diğer modüllerin (Concepts, Production) online LLM'e eriştiği GENEL arayüz. Model kataloğundan modeli bulur, kullanıcının
/// şifreli anahtarını çözer, uygun somut modeli (Anthropic / OpenAI / OpenRouter / GLM / Kimi) çalıştırır; her çağrı
/// CallLog'a maliyetiyle yazılır. Bütçe kontrolü burada YAPILMAZ: yeni iş başlatmadan önce <see cref="IBudgetGuard"/> kullanın.
/// Önemli üyeler: <see cref="RunStructuredAsync{T}"/> (JSON şema + tek onarım denemesi), <see cref="RunTextAsync"/>.
/// </summary>
public interface IProviderGateway
{
    /// <summary>Yapılandırılmış JSON çıktı ister ve <typeparamref name="T"/>'ye çözer (şema <typeparamref name="T"/>'den üretilir;
    /// <typeparamref name="T"/> bir nesne türü olmalı). JSON geçersizse bir kez onarım çağrısı yapılır.</summary>
    /// <typeparam name="T">Çıktı türü.</typeparam>
    /// <param name="task">İş.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Tipli sonuç ya da hata (<c>providers.*</c> kodları).</returns>
    Task<Result<TextResult<T>>> RunStructuredAsync<T>(TextTask task, CancellationToken ct);

    /// <summary>Serbest metin çıktı ister (ör. sahne kodu).</summary>
    /// <param name="task">İş.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Metin sonucu ya da hata.</returns>
    Task<Result<TextResult<string>>> RunTextAsync(TextTask task, CancellationToken ct);
}
