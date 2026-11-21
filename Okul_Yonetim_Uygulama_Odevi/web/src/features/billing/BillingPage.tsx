import { useState } from "react";
import type { AuthSession, Me } from "@okul/api-client";
import { t } from "../../i18n/t";
import { DiscountsTab } from "./DiscountsTab";
import { StudentAccount } from "./StudentAccount";
import { StudentAccountsTab } from "./StudentAccountsTab";
import { TariffsTab } from "./TariffsTab";

interface Props {
  me: Me;
  client: AuthSession["client"];
  route: string;
}

type Tab = "students" | "tariffs" | "discounts";

/**
 * Tahsilat sayfası (spec 0015, web; kurucu ve muhasebe): "Öğrenciler" (cari listesi) sekmesi herkese, "Tarifeler" ve "İndirimler"
 * yalnızca kurucuya (muhasebede gizli — sunucu da yazmayı reddeder). "#/tahsilat/ogrenci/&lt;id&gt;" öğrencinin carisi. Kurucu ya da muhasebe
 * olmayan kişi "Bu sayfaya erişiminiz yok." görür (sunucu her isteği ayrıca rol ve şubeyle denetler).
 */
export function BillingPage({ me, client, route }: Props) {
  const [tab, setTab] = useState<Tab>("students");
  const isFounder = me.roles.includes("Founder");
  if (!isFounder && !me.roles.includes("Accounting")) {
    return (
      <section className="card">
        <p role="alert">{t("billing.noAccess")}</p>
        <div className="actions">
          <a href="#/">{t("billing.back")}</a>
        </div>
      </section>
    );
  }

  const studentMatch = /^\/tahsilat\/ogrenci\/([0-9a-fA-F-]{36})$/.exec(route.split("?")[0]);
  if (studentMatch) {
    return <StudentAccount key={studentMatch[1]} client={client} studentId={studentMatch[1]} />;
  }

  const tabs: readonly Tab[] = isFounder ? ["students", "tariffs", "discounts"] : ["students"];
  return (
    <section className="card admin billing" aria-labelledby="billing-title">
      <div className="admin-head">
        <h2 id="billing-title">{t("billing.title")}</h2>
        <a href="#/">{t("billing.back")}</a>
      </div>
      <div role="tablist" aria-label={t("billing.title")} className="tabs">
        {tabs.map((name) => (
          <button key={name} type="button" role="tab" id={`billing-tab-${name}`} aria-selected={tab === name} aria-controls="panel-billing" onClick={() => setTab(name)}>
            {t(`billing.tabs.${name}`)}
          </button>
        ))}
      </div>
      <div role="tabpanel" id="panel-billing" aria-labelledby={`billing-tab-${tab}`}>
        {tab === "students" && <StudentAccountsTab client={client} />}
        {tab === "tariffs" && isFounder && <TariffsTab client={client} />}
        {tab === "discounts" && isFounder && <DiscountsTab client={client} />}
      </div>
    </section>
  );
}
