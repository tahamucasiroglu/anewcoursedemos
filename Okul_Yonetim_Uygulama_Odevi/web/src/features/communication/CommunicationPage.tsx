import { useState } from "react";
import type { AuthSession, Me } from "@okul/api-client";
import { t } from "../../i18n/t";
import { AnnouncementDetail } from "./AnnouncementDetail";
import { AnnouncementForm } from "./AnnouncementForm";
import { AnnouncementsList } from "./AnnouncementsList";

type View = { kind: "list" } | { kind: "new" } | { kind: "detail"; id: string };

interface Props {
  me: Me;
  client: AuthSession["client"];
  announcementId?: string;
}

/**
 * İletişim sayfası (spec 0011, "#/iletisim", yalnızca kurucu ve öğretmen; değilse "Bu sayfaya erişiminiz yok." ve istek
 * gitmez): duyuru listesi (kurucuda okulun bütün duyuruları, öğretmende kendi yayınladıkları) → "Yeni duyuru" formu →
 * yayınlanınca ayrıntı (okundu X / Y, okumayanlar, onay sonuçları). Liste dönüşte yeniden okunur. Spec 0017: "#/iletisim/duyuru/&lt;id&gt;"
 * (yönetici panelinin okunmayan duyurular kartı) doğrudan ayrıntıyı açar.
 */
export function CommunicationPage({ me, client, announcementId }: Props) {
  const isFounder = me.roles.includes("Founder");
  const allowed = isFounder || me.roles.includes("Teacher");
  const [view, setView] = useState<View>(announcementId ? { kind: "detail", id: announcementId } : { kind: "list" });

  return (
    <section className="card admin" aria-labelledby="communication-title">
      <div className="admin-head">
        <h2 id="communication-title">{t("communication.title")}</h2>
        <a href="#/">{t("communication.back")}</a>
      </div>
      {!allowed && <p className="form-error" role="alert">{t("communication.forbidden")}</p>}
      {allowed && view.kind === "list" && (
        <AnnouncementsList client={client} onNew={() => setView({ kind: "new" })} onOpen={(id) => setView({ kind: "detail", id })} />
      )}
      {allowed && view.kind === "new" && (
        <AnnouncementForm
          key="new"
          client={client}
          isFounder={isFounder}
          branches={me.branches}
          onCancel={() => setView({ kind: "list" })}
          onPublished={(id) => setView({ kind: "detail", id })}
        />
      )}
      {allowed && view.kind === "detail" && (
        <AnnouncementDetail key={view.id} client={client} id={view.id} onBack={() => setView({ kind: "list" })} />
      )}
    </section>
  );
}
