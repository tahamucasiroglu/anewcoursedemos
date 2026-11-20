import { useEffect, useState } from "react";
import { formatDay, turkeyToday, type AuthSession, type StaffAnnouncementRow } from "@okul/api-client";
import { t } from "../../i18n/t";
import { failureText } from "../admin/adminText";
import { call } from "../admin/adminApi";
import { fill, scopeText } from "./communicationText";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; rows: StaffAnnouncementRow[] };

interface Props {
  client: AuthSession["client"];
  onNew: () => void;
  onOpen: (id: string) => void;
}

/**
 * Duyuru listesi (spec 0011): yeniden eskiye; her satırda başlık, kapsam, yayınlayan, tarih, onay formu mu ve "X / Y veli
 * okudu". Durumlar: yükleniyor, boş ("Henüz duyuru yok"), dolu, hata + tekrar dene.
 */
export function AnnouncementsList({ client, onNew, onOpen }: Props) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/announcements")).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", rows: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  return (
    <>
      <div className="actions">
        <button type="button" onClick={onNew}>{t("communication.new")}</button>
      </div>
      {state.kind === "loading" && <p className="notice" aria-busy="true">{t("communication.loading")}</p>}
      {state.kind === "error" && (
        <>
          <p className="form-error" role="alert">{state.message}</p>
          <div className="actions">
            <button type="button" onClick={() => { setState({ kind: "loading" }); setReload((n) => n + 1); }}>{t("communication.retry")}</button>
          </div>
        </>
      )}
      {state.kind === "ready" && (state.rows.length === 0 ? (
        <p>{t("communication.empty")}</p>
      ) : (
        <ul className="calendar-list notice-list">
          {state.rows.map((row) => (
            <li key={row.id}>
              <button type="button" className="link-button" onClick={() => onOpen(row.id)}>{row.title}</button>
              <p>
                {scopeText(row.scope)} · {row.authorName} · {formatDay(turkeyToday(new Date(row.publishedAt)))}
                {row.requiresConsent && <> · {t("communication.consentForm")}</>}
              </p>
              <p>{fill(t("communication.readCount"), { read: row.readCount, total: row.recipientCount })}</p>
            </li>
          ))}
        </ul>
      ))}
    </>
  );
}
