import { useEffect, useState } from "react";
import { callApi, WEEK_CHOICES, type AuthSession, type Me, type WeekChoice, type WeeklySummaryClassroom } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { StudentStatusList } from "./StudentStatusList";
import { SummaryPanel } from "./SummaryPanel";

type State =
  | { kind: "loading" }
  | { kind: "error"; message: string; forbidden: boolean }
  | { kind: "ready"; rows: WeeklySummaryClassroom[] };

interface Props {
  me: Me;
  client: AuthSession["client"];
}

/**
 * Haftalık gelişim özeti sayfası (spec 0014, "#/haftalik-ozet", yalnızca öğretmen; değilse "Bu sayfaya erişiminiz yok."):
 * sınıf seç (tek sınıfsa doğrudan) → hafta seç ("Bu hafta" varsayılan / "Geçen hafta") → öğrenci listesi (durumlarla) →
 * öğrenci paneli (dayanak, taslak, onay) → "Öğrenci listesine dön" (liste yenilenir). Durumlar: yükleniyor, "Size atanmış sınıf
 * yok.", hata + tekrar dene. Liste ve panel seçime bağlı anahtarla yeniden kurulur (conventions T15).
 */
export function WeeklySummaryPage({ me, client }: Props) {
  const isTeacher = me.roles.includes("Teacher");
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [classroomId, setClassroomId] = useState<string | null>(null);
  const [week, setWeek] = useState<WeekChoice>("current");
  const [studentId, setStudentId] = useState<string | null>(null);
  const [listVersion, setListVersion] = useState(0);

  useEffect(() => {
    if (!isTeacher) {
      return;
    }

    let active = true;
    callApi(client.GET("/api/weekly-summaries/classrooms")).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", rows: outcome.data });
      } else {
        const forbidden = !outcome.unreachable && outcome.status === 403;
        setState({ kind: "error", forbidden, message: forbidden ? t("weekly.forbidden") : outcome.unreachable ? t("weekly.unreachable") : outcome.message ?? t("weekly.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload, isTeacher]);

  const rows = state.kind === "ready" ? state.rows : [];
  const selected = classroomId ?? (rows.length === 1 ? rows[0].id : null);
  const classroom = rows.find((row) => row.id === selected) ?? null;
  return (
    <section className="card admin" aria-labelledby="weekly-title">
      <div className="admin-head">
        <h2 id="weekly-title">{t("weekly.title")}</h2>
        <a href="#/">{t("weekly.back")}</a>
      </div>
      {!isTeacher && <p className="form-error" role="alert">{t("weekly.forbidden")}</p>}
      {isTeacher && state.kind === "loading" && <p className="notice" aria-busy="true">{t("weekly.loading")}</p>}
      {isTeacher && state.kind === "error" && (
        <>
          <p className="form-error" role="alert">{state.message}</p>
          {!state.forbidden && (
            <div className="actions">
              <button type="button" onClick={() => { setState({ kind: "loading" }); setReload((n) => n + 1); }}>{t("weekly.retry")}</button>
            </div>
          )}
        </>
      )}
      {isTeacher && state.kind === "ready" && rows.length === 0 && <p>{t("weekly.noClassrooms")}</p>}
      {isTeacher && state.kind === "ready" && rows.length > 0 && (
        <>
          <div className="weekly-filters">
            {rows.length > 1 && (
              <label>
                {t("weekly.classroom")}
                <select
                  value={selected ?? ""}
                  onChange={(event) => {
                    setClassroomId(event.target.value || null);
                    setStudentId(null);
                  }}
                >
                  <option value="">{t("weekly.chooseClassroom")}</option>
                  {rows.map((row) => (
                    <option key={row.id} value={row.id}>{row.name} · {row.branchName}</option>
                  ))}
                </select>
              </label>
            )}
            <fieldset className="weekly-week">
              <legend>{t("weekly.week")}</legend>
              {WEEK_CHOICES.map((choice) => (
                <label key={choice}>
                  <input
                    type="radio"
                    name="weekly-week"
                    value={choice}
                    checked={week === choice}
                    onChange={() => {
                      setWeek(choice);
                      setStudentId(null);
                    }}
                  />
                  {t(`weekly.week.${choice}` as TextKey)}
                </label>
              ))}
            </fieldset>
          </div>
          {classroom && (studentId ? (
            <>
              <SummaryPanel key={`${studentId}:${week}`} client={client} studentId={studentId} week={week} />
              <div className="actions">
                <button type="button" className="secondary" onClick={() => { setStudentId(null); setListVersion((n) => n + 1); }}>{t("weekly.backToList")}</button>
              </div>
            </>
          ) : (
            <StudentStatusList key={`${classroom.id}:${week}:${listVersion}`} client={client} classroomId={classroom.id} week={week} onOpen={setStudentId} />
          ))}
        </>
      )}
    </section>
  );
}
