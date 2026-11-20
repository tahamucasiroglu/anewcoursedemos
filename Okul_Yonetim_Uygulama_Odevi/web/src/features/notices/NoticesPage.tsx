import { useEffect, useRef, useState } from "react";
import { callApi, formatDay, noticeBadges, type AuthSession, type Me, type NoticeView } from "@okul/api-client";
import { t } from "../../i18n/t";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; notices: NoticeView[] };

interface Props {
  me: Me;
  client: AuthSession["client"];
  onRead: () => void;
}

/**
 * Velinin bildirimleri (spec 0010, "#/bildirimler", yalnızca veli; değilse "Bu sayfaya erişiminiz yok."): yalnızca kendi
 * çocuklarınınkiler, en yenisi üstte, en fazla 50; okunmamışlar "Yeni" etiketi ve kalın yazıyla, düzeltilen yoklama
 * "Düzeltildi" etiketiyle. Liste gelince hepsi okundu yapılır ve Hoş geldin'deki sayı yenilenir (<c>onRead</c>). Durumlar:
 * yükleniyor, boş ("Henüz bildirim yok"), dolu, hata + tekrar dene.
 */
export function NoticesPage({ me, client, onRead }: Props) {
  const isGuardian = me.roles.includes("Guardian");
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const readCallback = useRef(onRead);
  useEffect(() => {
    readCallback.current = onRead;
  }, [onRead]);

  useEffect(() => {
    if (!isGuardian) {
      return;
    }

    let active = true;
    callApi(client.GET("/api/notices")).then(async (outcome) => {
      if (!active) {
        return;
      }

      if (!outcome.ok) {
        setState({ kind: "error", message: outcome.unreachable ? t("notices.unreachable") : outcome.message ?? t("notices.loadError") });
        return;
      }

      setState({ kind: "ready", notices: outcome.data });
      if (outcome.data.some((n) => !n.isRead) && (await callApi(client.POST("/api/notices/read"))).ok && active) {
        readCallback.current();
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload, isGuardian]);

  return (
    <section className="card admin" aria-labelledby="notices-title">
      <div className="admin-head">
        <h2 id="notices-title">{t("notices.title")}</h2>
        <a href="#/">{t("notices.back")}</a>
      </div>
      {!isGuardian && <p className="form-error" role="alert">{t("attendance.forbidden")}</p>}
      {isGuardian && state.kind === "loading" && <p className="notice" aria-busy="true">{t("notices.loading")}</p>}
      {isGuardian && state.kind === "error" && (
        <>
          <p className="form-error" role="alert">{state.message}</p>
          <div className="actions">
            <button type="button" onClick={() => { setState({ kind: "loading" }); setReload((n) => n + 1); }}>{t("notices.retry")}</button>
          </div>
        </>
      )}
      {isGuardian && state.kind === "ready" && (state.notices.length === 0 ? (
        <p>{t("notices.empty")}</p>
      ) : (
        <ul className="calendar-list notice-list">
          {state.notices.map((notice) => {
            const { isNew, corrected } = noticeBadges(notice);
            return (
              <li key={notice.id} className={isNew ? "unread" : undefined}>
                {isNew && <span className="badge">{t("notices.new")} · </span>}
                {corrected && <span className="badge">{t("notices.corrected")} · </span>}
                <span className="badge">{formatDay(notice.day)}</span>
                <p>{notice.text}</p>
              </li>
            );
          })}
        </ul>
      ))}
    </section>
  );
}
