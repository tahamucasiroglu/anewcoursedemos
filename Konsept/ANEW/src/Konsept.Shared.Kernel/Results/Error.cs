using Konsept.Shared.Kernel.Enums;

namespace Konsept.Shared.Kernel.Results;

/// <summary>
/// Beklenen bir iş hatası: makinece okunur kod, kullanıcıya gösterilecek Türkçe mesaj ve tür.
/// </summary>
/// <param name="Code">Kararlı hata kodu (ör. <c>agent.not_connected</c>).</param>
/// <param name="Message">Kullanıcıya gösterilecek sade Türkçe mesaj.</param>
/// <param name="Type">HTTP durum kodunu belirleyen hata türü.</param>
public sealed record Error(string Code, string Message, ErrorType Type);
