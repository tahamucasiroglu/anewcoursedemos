using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration;

/// <summary>
/// Formdaki rol seçimini atama listesine çevirir ve doğrular (spec 0005 gereksinim 3): kurucu/yönetici okul düzeyinde
/// (şubesiz), diğer roller en az bir etkin şubeyle. Şube seçili okulda değilse ya da pasifse reddedilir.
/// </summary>
public static class RolePlan
{
    public static async Task<IReadOnlyList<(Role Role, Guid? BranchId)>> BuildAsync(
        IReadOnlyList<RoleInput> inputs,
        IBranchRepository branches,
        ILocalizationService localization,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputs);
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
                if (branch is null || !branch.IsActive)
                {
                    throw new BusinessException(await localization.GetAsync(AdministrationMessages.BranchUnavailable).ConfigureAwait(false));
                }

                plan.Add((role, branchId));
            }
        }

        return [.. plan.Distinct()];
    }
}
