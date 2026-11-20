import { useState } from "react";
import type { AuthSession, Me } from "@okul/api-client";
import { t } from "../../i18n/t";
import { StudentCard } from "./StudentCard";
import { StudentForm } from "./StudentForm";
import { StudentsList } from "./StudentsList";

interface Props {
  me: Me;
  client: AuthSession["client"];
}

type View = { kind: "list" } | { kind: "add" } | { kind: "card"; id: string };

/**
 * Öğrenciler sayfası (spec 0009, "#/ogrenciler"; yalnızca web ve yalnızca kurucu/yönetici): liste → "Öğrenci ekle" →
 * öğrenci kartı; karttaki kardeş satırı o kardeşin kartını açar. Kurucu olmayan kişi "Bu sayfaya erişiminiz yok." görür
 * (sunucu da her işlemi ayrıca reddeder).
 */
export function StudentsPage({ me, client }: Props) {
  const [view, setView] = useState<View>({ kind: "list" });
  if (!me.roles.includes("Founder")) {
    return (
      <section className="card">
        <p role="alert">{t("admin.noAccess")}</p>
        <div className="actions">
          <a href="#/">{t("students.back")}</a>
        </div>
      </section>
    );
  }

  return (
    <section className="card admin" aria-labelledby="students-title">
      <div className="admin-head">
        <h2 id="students-title">{t("students.title")}</h2>
        <a href="#/">{t("students.back")}</a>
      </div>
      {view.kind === "list" && <StudentsList client={client} onAdd={() => setView({ kind: "add" })} onOpen={(id) => setView({ kind: "card", id })} />}
      {view.kind === "add" && <StudentForm client={client} onCancel={() => setView({ kind: "list" })} onSaved={(id) => setView({ kind: "card", id })} />}
      {view.kind === "card" && <StudentCard key={view.id} client={client} studentId={view.id} onBack={() => setView({ kind: "list" })} onOpen={(id) => setView({ kind: "card", id })} />}
    </section>
  );
}
