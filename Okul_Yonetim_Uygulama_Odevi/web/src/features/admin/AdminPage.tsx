import { useState } from "react";
import type { AuthSession, Me } from "@okul/api-client";
import { t } from "../../i18n/t";
import { BranchesTab } from "./BranchesTab";
import { MembersTab } from "./MembersTab";

interface Props {
  me: Me;
  client: AuthSession["client"];
}

/**
 * Yönetim sayfası (spec 0005, yalnızca web ve yalnızca kurucu/yönetici): Şubeler ve Kullanıcılar sekmeleri. Kurucu
 * olmayan kişi bu adrese gelirse "Bu sayfaya erişiminiz yok." görür (sunucu da her işlemi ayrıca reddeder).
 */
export function AdminPage({ me, client }: Props) {
  const [tab, setTab] = useState<"branches" | "members">("branches");
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
        <button type="button" role="tab" id="tab-branches" aria-selected={tab === "branches"} aria-controls="panel-admin" onClick={() => setTab("branches")}>
          {t("admin.tabs.branches")}
        </button>
        <button type="button" role="tab" id="tab-members" aria-selected={tab === "members"} aria-controls="panel-admin" onClick={() => setTab("members")}>
          {t("admin.tabs.members")}
        </button>
      </div>
      <div role="tabpanel" id="panel-admin" aria-labelledby={tab === "branches" ? "tab-branches" : "tab-members"}>
        {tab === "branches" ? <BranchesTab client={client} /> : <MembersTab client={client} />}
      </div>
    </section>
  );
}
