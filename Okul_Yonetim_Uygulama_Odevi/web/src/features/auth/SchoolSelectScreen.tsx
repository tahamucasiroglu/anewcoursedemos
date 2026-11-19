import type { TenantSummary } from "@okul/api-client";
import { t } from "../../i18n/t";

interface Props {
  tenants: TenantSummary[];
  selecting: boolean;
  failed: boolean;
  onSelect: (tenantId: string) => void;
}

/** Okul seçimi (spec 0004): kişinin okulları düğme listesi; seçilirken düğmeler kilitli; hata duyurulur. */
export function SchoolSelectScreen({ tenants, selecting, failed, onSelect }: Props) {
  return (
    <section className="card" aria-labelledby="school-title" aria-busy={selecting}>
      <h2 id="school-title">{t("school.title")}</h2>
      {tenants.length === 0 ? (
        <p>{t("school.none")}</p>
      ) : (
        <ul className="school-list">
          {tenants.map((tenant) => (
            <li key={tenant.id}>
              <button type="button" disabled={selecting} onClick={() => onSelect(tenant.id)}>
                {tenant.name}
              </button>
            </li>
          ))}
        </ul>
      )}
      {selecting && <p role="status">{t("school.loading")}</p>}
      {failed && (
        <p className="form-error" role="alert">
          {t("school.error")}
        </p>
      )}
    </section>
  );
}
