import type { AuthSession, Me } from "@okul/api-client";
import { t } from "../../i18n/t";
import { ReportsTab } from "./ReportsTab";
import { SummaryTab } from "./SummaryTab";

interface Props {
  me: Me;
  client: AuthSession["client"];
  route: string;
}

type Tab = "summary" | "reports";

/**
 * Yönetici paneli (spec 0017, web, yalnızca kurucu): "#/panel" günlük özet, "#/panel/raporlar" raporlar. Kurucu olmayan kişi "Bu sayfaya
 * erişiminiz yok." görür ve istek gitmez (sunucu her isteği ayrıca rolle denetler). Sekmeler adres değiştirir (geri tuşu çalışır); geniş
 * tablolar için sayfa genişler, dar ekranda tablolar kendi içinde kayar.
 */
export function DashboardPage({ me, client, route }: Props) {
  if (!me.roles.includes("Founder")) {
    return (
      <section className="card">
        <p role="alert">{t("dashboard.noAccess")}</p>
        <div className="actions">
          <a href="#/">{t("dashboard.back")}</a>
        </div>
      </section>
    );
  }

  const tab: Tab = route.split("?")[0] === "/panel/raporlar" ? "reports" : "summary";
  const tabs: readonly Tab[] = ["summary", "reports"];
  return (
    <section className="card admin dashboard" aria-labelledby="dashboard-title">
      <div className="admin-head">
        <h2 id="dashboard-title">{t("dashboard.title")}</h2>
        <a href="#/">{t("dashboard.back")}</a>
      </div>
      <div role="tablist" aria-label={t("dashboard.title")} className="tabs">
        {tabs.map((name) => (
          <button
            key={name}
            type="button"
            role="tab"
            id={`dashboard-tab-${name}`}
            aria-selected={tab === name}
            aria-controls="panel-dashboard"
            onClick={() => {
              window.location.hash = name === "reports" ? "/panel/raporlar" : "/panel";
            }}
          >
            {t(`dashboard.tabs.${name}`)}
          </button>
        ))}
      </div>
      <div role="tabpanel" id="panel-dashboard" aria-labelledby={`dashboard-tab-${tab}`}>
        {tab === "summary" ? <SummaryTab client={client} branches={me.branches} /> : <ReportsTab key={route} client={client} branches={me.branches} route={route} />}
      </div>
    </section>
  );
}
