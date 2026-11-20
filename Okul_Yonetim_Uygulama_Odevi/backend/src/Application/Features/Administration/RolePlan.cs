using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration;

/// <summary>
/// Formdaki rol seçimini atama listesine çevirir ve doğrular (spec 0005 gereksinim 3): kurucu/yönetici okul düzeyinde
/// (şubesiz), diğer roller en az bir etkin şubeyle. Şube seçili okulda değilse reddedilir; pasif şube yalnızca kişinin
/// zaten var olan ataması olarak kalabilir (review M-1: şube pasifleşince kişinin rolleri düzenlenebilir kalır), yeni
/// atamada seçilemez (AC-4).
/// </summary>
public static class RolePlan
{
    public static async Task<IReadOnlyList<(Role Role, Guid? BranchId)>> BuildAsync(
        IReadOnlyList<RoleInput> inputs,
        IReadOnlyCollection<(Role Role, Guid? BranchId)> existing,
        IBranchRepository branches,
        ILocalizationService localization,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(branches);
        List<(Role, Guid?)> plan = [];
        foreach (RoleInput input in inputs)
        {
            if (!Enum.TryParse(input.Role, ignoreCase: false, out Role role) || !Enum.IsDefined(role) || int.TryParse(input.Role, out _))
            {
                throw new BusinessException(await localization.GetAsync(AdministrationMessages.RoleInvalid).ConfigureAwait(false));
            }

            if (role == Role.Founder)
            {
                plan.Add((role, null));
                continue;
            }

            if (input.BranchIds is null || input.BranchIds.Count == 0)
            {
                throw new BusinessException(await localization.GetAsync(AdministrationMessages.BranchRequiredForRole).ConfigureAwait(false));
            }

            foreach (Guid branchId in input.BranchIds.Distinct())
            {
                Branch? branch = await branches.FindVisibleAsync(branchId, cancellationToken).ConfigureAwait(false);
                if (branch is null || (!branch.IsActive && !existing.Contains((role, branchId))))
                {
                    throw new BusinessException(await localization.GetAsync(AdministrationMessages.BranchUnavailable).ConfigureAwait(false));
                }

                plan.Add((role, branchId));
            }
        }

        return [.. plan.Distinct()];
    }
}
