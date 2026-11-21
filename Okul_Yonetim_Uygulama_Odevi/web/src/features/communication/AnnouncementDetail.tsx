import { useEffect, useState } from "react";
import { consentCounts, formatDay, formatTurkeyDateTime, turkeyToday, type AnnouncementDetail as Detail, type AnnouncementResults, type AuthSession } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { answerText, fill, scopeText } from "./communicationText";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; detail: Detail; results: AnnouncementResults };

interface Props {
  client: AuthSession["client"];
  id: string;
  onBack: () => void;
}

/**
 * Duyuru ayrıntısı ve sonuçları (spec 0011 gereksinim 3, 4): başlık, metin, kapsam, yayınlayan; "X / Y veli okudu" ve
 * okumayan veliler (ad + çocuk adları); onay formunda soru, son gün, onay / ret / cevapsız sayıları ve öğrenci listesi (cevabı kim, ne zaman verdi — Türkiye saati).
 */
export function AnnouncementDetail({ client, id, onBack }: Props) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    Promise.all([
      call(client.GET("/api/announcements/{id}", { params: { path: { id } } })),
      call(client.GET("/api/announcements/{id}/results", { params: { path: { id } } })),
    ]).then(([detail, results]) => {
      if (!active) {
        return;
      }

      if (detail.ok && results.ok) {
        setState({ kind: "ready", detail: detail.data, results: results.data });
      } else {
        setState({ kind: "error", message: failureText(!detail.ok ? detail : results.ok ? { unreachable: true } : results) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, id, reload]);

  const detail = state.kind === "ready" ? state.detail : null;
  const results = state.kind === "ready" ? state.results : null;
  const consent = results?.consent ? consentCounts(results.consent) : null;
  return (
    <div>
      <div className="actions">
        <button type="button" className="secondary" onClick={onBack}>{t("communication.backToList")}</button>
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
      {detail && results && (
        <article className="announcement" aria-labelledby="announcement-heading">
          <h3 id="announcement-heading">{detail.title}</h3>
          <p className="badge">
            {scopeText(detail.scope)} · {detail.authorName} · {formatDay(turkeyToday(new Date(detail.publishedAt)))}
          </p>
          <p className="announcement-body">{detail.body}</p>

          <h4>{fill(t("communication.readCount"), { read: results.readCount, total: results.recipientCount })}</h4>
          {results.unread.length > 0 && (
            <>
              <p>{t("communication.unreadTitle")}</p>
              <ul className="calendar-list notice-list">
                {results.unread.map((u, i) => (
                  <li key={`${u.guardianName}-${i}`}>{u.guardianName}{u.childNames.length > 0 && ` (${u.childNames.join(", ")})`}</li>
                ))}
              </ul>
            </>
          )}

          {consent && results.consent && (
            <>
              <h4>{t("communication.consentTitle")}</h4>
              <p>{detail.consentQuestion}</p>
              {detail.consentDeadline && <p>{fill(t("communication.deadlineText"), { day: formatDay(detail.consentDeadline) })}</p>}
              <p>{fill(t("communication.consentCounts"), consent)}</p>
              <ul className="calendar-list notice-list">
                {results.consent.students.map((s) => (
                  <li key={s.studentId}>
                    {s.studentName}: {answerText(s.answer)}
                    {s.answeredByName && ` — ${s.answeredByName}`}
                    {s.answeredByName && s.answeredAt && ` · ${formatTurkeyDateTime(s.answeredAt)}`}
                  </li>
                ))}
              </ul>
            </>
          )}
        </article>
      )}
    </div>
  );
}
