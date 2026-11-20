import { ROLE_NAMES, type BranchAdminView, type RoleInput, type RoleName } from "@okul/api-client";
import { t } from "../../i18n/t";
import { roleText } from "./adminText";

interface Props {
  idPrefix: string;
  value: RoleInput[];
  branches: BranchAdminView[];
  onChange: (value: RoleInput[]) => void;
  lockedRole?: { role: RoleName; reason: string } | null;
  errorId?: string;
}

/**
 * Rol ve şube seçimi (spec 0005): kurucu/yönetici okul düzeyinde (şube seçilmez); diğer roller için etkin şubeler
 * işaretlenir. Pasif şube yalnızca kişinin zaten var olan ataması olarak "pasif" etiketiyle görünür ve kaldırılabilir
 * (review M-1); yeni atamada seçilemez. Kilitli rol (ör. kendi yönetici rolünüz) kaldırılamaz, nedeni yanında yazar.
 * Rol hatası alana bağlanır (review L-5).
 */
export function RolesEditor({ idPrefix, value, branches, onChange, lockedRole, errorId }: Props) {
  const find = (role: RoleName) => value.find((r) => r.role === role);
  const toggleRole = (role: RoleName, checked: boolean) =>
    onChange(checked ? [...value, { role, branchIds: [] }] : value.filter((r) => r.role !== role));
  const toggleBranch = (role: RoleName, branchId: string, checked: boolean) =>
    onChange(value.map((r) => (r.role !== role ? r : { ...r, branchIds: checked ? [...r.branchIds, branchId] : r.branchIds.filter((b) => b !== branchId) })));

  return (
    <fieldset className="roles" aria-describedby={errorId}>
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
                {branches.filter((b) => b.isActive || selected.branchIds.includes(b.id)).map((branch) => {
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
                      {!branch.isActive && <span className="badge"> ({t("branches.inactive")})</span>}
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
