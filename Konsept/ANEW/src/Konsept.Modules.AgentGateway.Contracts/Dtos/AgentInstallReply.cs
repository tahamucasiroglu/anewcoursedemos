namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Kurulum komutunun sonucu (spec 0027 Req 5, 7): hepsi kurulduysa <c>Ok</c>; kurulamayanların kimlikleri.</summary>
/// <param name="Ok">İstenen bileşenlerin hepsi kurulu ve sağlam mı.</param>
/// <param name="Failed">Kurulamayan (ya da listede olmayan) bileşenler.</param>
public sealed record AgentInstallReply(bool Ok, IReadOnlyList<string> Failed);
