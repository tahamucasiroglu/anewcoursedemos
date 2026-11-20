import { useState } from "react";
import type { AuthSession, Me, PanelSchoolResult } from "@okul/api-client";
import { t } from "../../i18n/t";
import { TempPasswordResult } from "../admin/TempPasswordResult";
import { SchoolForm } from "./SchoolForm";
import { SchoolsList } from "./SchoolsList";

interface Props {
  me: Me;
  client: AuthSession["client"];
}

type View = { kind: "list" } | { kind: "add" } | { kind: "result"; result: PanelSchoolResult };

/**
 * Platform paneli (spec 0007, yalnızca web, yalnızca platform yöneticisi + kodla açılmış oturum): okul listesi, okul ekle
 * ve sonuç (yeni kurucuya geçici şifre bir kez ya da "zaten hesabı var"). Diğer kişiler "Bu sayfaya erişiminiz yok." görür
 * (sunucu da her isteği ayrıca reddeder). Okulların içindeki veri burada yok.
 */
export function PlatformPage({ me, client }: Props) {
  const [view, setView] = useState<View>({ kind: "list" });
  if (!me.isPlatformAdmin) {
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
    <section className="card admin" aria-labelledby="platform-title">
      <div className="admin-head">
        <h2 id="platform-title">{t("platform.title")}</h2>
        <a href="#/">{t("admin.back")}</a>
      </div>
      {view.kind === "list" && <SchoolsList client={client} onAdd={() => setView({ kind: "add" })} />}
      {view.kind === "add" && <SchoolForm client={client} onCancel={() => setView({ kind: "list" })} onCreated={(result) => setView({ kind: "result", result })} />}
      {view.kind === "result" &&
        (view.result.temporaryPassword ? (
          <TempPasswordResult password={view.result.temporaryPassword} onDone={() => setView({ kind: "list" })} />
        ) : (
          <div className="card">
            <p role="status">{t("platform.existingAccount")}</p>
            <div className="actions">
              <button type="button" onClick={() => setView({ kind: "list" })}>{t("platform.backToList")}</button>
            </div>
          </div>
        ))}
    </section>
  );
}
