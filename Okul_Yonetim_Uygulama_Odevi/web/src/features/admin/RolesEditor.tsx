import { ROLE_NAMES, type BranchAdminView, type RoleInput, type RoleName } from "@okul/api-client";
import { t } from "../../i18n/t";
import { roleText } from "./adminText";

interface Props {
  idPrefix: string;
  value: RoleInput[];
  branches: BranchAdminView[];
  onChange: (value: RoleInput[]) => void;
  lockedRole?: { role: RoleName; reason: string } | null;
}

/**
 * Rol ve şube seçimi (spec 0005): kurucu/yönetici okul düzeyinde (şube seçilmez); diğer roller için etkin şubeler
 * işaretlenir. Kilitli rol (ör. kendi yönetici rolünüz) kaldırılamaz, nedeni yanında yazar.
 */
export function RolesEditor({ idPrefix, value, branches, onChange, lockedRole }: Props) {
  const active = branches.filter((b) => b.isActive);
  const find = (role: RoleName) => value.find((r) => r.role === role);
  const toggleRole = (role: RoleName, checked: boolean) =>
    onChange(checked ? [...value, { role, branchIds: [] }] : value.filter((r) => r.role !== role));
  const toggleBranch = (role: RoleName, branchId: string, checked: boolean) =>
    onChange(value.map((r) => (r.role !== role ? r : { ...r, branchIds: checked ? [...r.branchIds, branchId] : r.branchIds.filter((b) => b !== branchId) })));

  return (
    <fieldset className="roles">
      <legend>{t("members.roles")}</legend>
      {ROLE_NAMES.map((role) => {
        const selected = find(role);
        const locked = lockedRole?.role === role;
        const roleId = `${idPrefix}-role-${role}`;
        return (
          <div key={role} className="role-row">
            <label htmlFor={roleId}>
              <input
                id={roleId}
                type="checkbox"
                checked={selected !== undefined}
                disabled={locked}
                aria-describedby={locked ? `${roleId}-reason` : undefined}
                onChange={(e) => toggleRole(role, e.target.checked)}
              />{" "}
              {roleText(role)}
            </label>
            {locked && (
              <span id={`${roleId}-reason`} className="notice">
                {lockedRole?.reason}
              </span>
            )}
            {selected && role !== "Founder" && (
              <div className="branch-choices" role="group" aria-label={`${roleText(role)} — ${t("members.branch")}`}>
                {active.map((branch) => {
                  const branchId = `${roleId}-${branch.id}`;
                  return (
                    <label key={branch.id} htmlFor={branchId}>
                      <input
                        id={branchId}
                        type="checkbox"
                        checked={selected.branchIds.includes(branch.id)}
                        onChange={(e) => toggleBranch(role, branch.id, e.target.checked)}
                      />{" "}
                      {branch.name}
                    </label>
                  );
                })}
              </div>
            )}
          </div>
        );
      })}
    </fieldset>
  );
}
