namespace Konsept.Modules.Concepts.Contracts.Abstract;

/// <summary>
/// Konsepti olan profiller (spec 0014 Req 26, 0003 Req 14): durumundan ve silinme işaretinden bağımsız en az bir konsepti olan profil
/// silinemez. Kullanıcı kimliğiyle süzülür (BR-1).
/// </summary>
public interface IProfileConceptPresence
{
    /// <summary>Kullanıcının konsepti olan profilleri.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil kimlikleri.</returns>
    Task<IReadOnlySet<Guid>> ProfilesWithConceptsAsync(Guid userId, CancellationToken cancellationToken);
}
