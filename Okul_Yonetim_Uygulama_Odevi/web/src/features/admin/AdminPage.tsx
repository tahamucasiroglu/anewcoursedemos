import { useState } from "react";
import type { AuthSession, Me } from "@okul/api-client";
import { t } from "../../i18n/t";
import { BranchesTab } from "./BranchesTab";
import { ClassroomsTab } from "./ClassroomsTab";
import { GradeLevelsTab } from "./GradeLevelsTab";
import { MembersTab } from "./MembersTab";
import { TermsTab } from "./TermsTab";

interface Props {
  me: Me;
  client: AuthSession["client"];
}

type Tab = "branches" | "members" | "gradeLevels" | "terms" | "classrooms";
const TABS: readonly Tab[] = ["branches", "members", "gradeLevels", "terms", "classrooms"];

/**
 * Yönetim sayfası (spec 0005, yalnızca web ve yalnızca kurucu/yönetici): Şubeler ve Kullanıcılar sekmeleri; spec 0008
 * ile Sınıf seviyeleri, Dönemler ve takvim, Sınıflar sekmeleri (dar ekranda sekmeler satır sarar). Kurucu
 * olmayan kişi bu adrese gelirse "Bu sayfaya erişiminiz yok." görür (sunucu da her işlemi ayrıca reddeder).
 */
export function AdminPage({ me, client }: Props) {
  const [tab, setTab] = useState<Tab>("branches");
  if (!me.roles.includes("Founder")) {
    return (
      <section className="card">
        <p role="alert">{t("admin.noAccess")}</p>
        <div className="actions">
          <a href="#/">{t("admin.back")}</a>
        </div>
      </section>
    );
  }

  return (
    <section className="card admin" aria-labelledby="admin-title">
      <div className="admin-head">
        <h2 id="admin-title">{t("admin.title")}</h2>
        <a href="#/">{t("admin.back")}</a>
      </div>
      <div role="tablist" aria-label={t("admin.title")} className="tabs">
        {TABS.map((name) => (
          <button key={name} type="button" role="tab" id={`tab-${name}`} aria-selected={tab === name} aria-controls="panel-admin" onClick={() => setTab(name)}>
            {t(`admin.tabs.${name}`)}
          </button>
        ))}
      </div>
      <div role="tabpanel" id="panel-admin" aria-labelledby={`tab-${tab}`}>
        {tab === "branches" && <BranchesTab client={client} />}
        {tab === "members" && <MembersTab client={client} />}
        {tab === "gradeLevels" && <GradeLevelsTab client={client} />}
        {tab === "terms" && <TermsTab client={client} />}
        {tab === "classrooms" && <ClassroomsTab client={client} />}
      </div>
    </section>
  );
}
