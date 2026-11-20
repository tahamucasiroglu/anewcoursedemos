import { useEffect, useState } from "react";
import { calendarKindKey, callApi, dateRangeText, type AuthSession, type SchoolCalendarView } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; calendar: SchoolCalendarView };

/**
 * Okul takvimi (spec 0008, "#/takvim", okulda rolü olan herkes, salt okunur): güncel dönemin adı ve tarihleri (yaklaşan
 * dönemse "Yaklaşan dönem"), altında kayıtlar (tür metni, başlık, tek gün ya da tarih aralığı) tarih sırasıyla. Dönem
 * yoksa "Henüz dönem tanımlanmadı.", kayıt yoksa "Bu dönemde takvim kaydı yok."; yükleniyor ve hata + tekrar dene.
 */
export function CalendarPage({ client }: { client: AuthSession["client"] }) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/calendar")).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", calendar: outcome.data });
      } else {
        setState({ kind: "error", message: outcome.unreachable ? t("calendar.unreachable") : outcome.message ?? t("calendar.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  return (
    <section className="card admin" aria-labelledby="calendar-title">
      <div className="admin-head">
        <h2 id="calendar-title">{t("calendar.title")}</h2>
        <a href="#/">{t("calendar.back")}</a>
      </div>
      {state.kind === "loading" && <p className="notice" aria-busy="true">{t("calendar.loading")}</p>}
      {state.kind === "error" && (
        <>
          <p className="form-error" role="alert">{state.message}</p>
          <div className="actions">
            <button
              type="button"
              onClick={() => {
                setState({ kind: "loading" });
                setReload((n) => n + 1);
              }}
            >
              {t("calendar.retry")}
            </button>
          </div>
        </>
      )}
      {state.kind === "ready" && <CalendarBody calendar={state.calendar} />}
    </section>
  );
}

function CalendarBody({ calendar }: { calendar: SchoolCalendarView }) {
  const term = calendar.term;
  if (!term) {
    return <p>{t("calendar.noTerm")}</p>;
  }

  return (
    <>
      <div>
        <h3>{term.name}</h3>
        <p className="notice">
          {dateRangeText(term.startDate, term.endDate)}
          {term.isUpcoming && <span className="badge"> · {t("calendar.upcoming")}</span>}
        </p>
      </div>
      {calendar.entries.length === 0 ? (
        <p>{t("calendar.empty")}</p>
      ) : (
        <ul className="calendar-list">
          {calendar.entries.map((entry) => (
            <li key={entry.id}>
              <span className="badge">{t(`calendar.kind.${calendarKindKey(entry.kind)}` as TextKey)}</span> <strong>{entry.title}</strong>
              <span className="notice"> · {dateRangeText(entry.startDate, entry.endDate)}</span>
            </li>
          ))}
        </ul>
      )}
    </>
  );
}
