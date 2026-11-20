import { useEffect, useState } from "react";
import { callApi, type AuthSession, type Me, type MyClassroomRow } from "@okul/api-client";
import { t } from "../../i18n/t";
import { AttendanceList } from "./AttendanceList";
import { MyClassroomsList } from "./MyClassroomsList";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; rows: MyClassroomRow[] };

interface Props {
  me: Me;
  client: AuthSession["client"];
}

/**
 * Yoklama sayfası (spec 0010, "#/yoklama", yalnızca öğretmen; değilse "Bu sayfaya erişiminiz yok."): Sınıflarım (bugün
 * alındı/alınmadı); tek sınıf varsa doğrudan o sınıfın yoklaması açılır, birden çoksa seçilen sınıfın yoklaması ve
 * "Sınıflarıma dön" (liste yenilenir). Durumlar: yükleniyor, boş ("Size atanmış sınıf yok"), dolu, hata + tekrar dene.
 */
export function AttendancePage({ me, client }: Props) {
  const isTeacher = me.roles.includes("Teacher");
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [selected, setSelected] = useState<string | null>(null);

  useEffect(() => {
    if (!isTeacher) {
      return;
    }

    let active = true;
    callApi(client.GET("/api/attendance/classrooms")).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", rows: outcome.data } : { kind: "error", message: outcome.unreachable ? t("attendance.unreachable") : outcome.message ?? t("attendance.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload, isTeacher]);

  const refresh = () => {
    setState({ kind: "loading" });
    setReload((n) => n + 1);
  };

  const only = state.kind === "ready" && state.rows.length === 1 ? state.rows[0].id : null;
  const open = selected ?? only;
  return (
    <section className="card admin" aria-labelledby="attendance-title">
      <div className="admin-head">
        <h2 id="attendance-title">{t("attendance.title")}</h2>
        <a href="#/">{t("attendance.back")}</a>
      </div>
      {!isTeacher && <p className="form-error" role="alert">{t("attendance.forbidden")}</p>}
      {isTeacher && state.kind === "loading" && <p className="notice" aria-busy="true">{t("attendance.loading")}</p>}
      {isTeacher && state.kind === "error" && (
        <>
          <p className="form-error" role="alert">{state.message}</p>
          <div className="actions">
            <button type="button" onClick={refresh}>{t("attendance.retry")}</button>
          </div>
        </>
      )}
      {isTeacher && state.kind === "ready" && (open ? (
        <>
          <AttendanceList key={open} client={client} classroomId={open} />
          {!only && (
            <div className="actions">
              <button type="button" className="secondary" onClick={() => { setSelected(null); refresh(); }}>{t("attendance.backToList")}</button>
            </div>
          )}
        </>
      ) : (
        <MyClassroomsList rows={state.rows} onOpen={setSelected} />
      ))}
    </section>
  );
}
