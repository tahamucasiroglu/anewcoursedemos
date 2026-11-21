import { useEffect, useState } from "react";
import { callApi, fillText, summaryStatusKey, type AuthSession, type WeekChoice, type WeeklySummaryList } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; list: WeeklySummaryList };

interface Props {
  client: AuthSession["client"];
  classroomId: string;
  week: WeekChoice;
  onOpen: (studentId: string) => void;
}

/**
 * Sınıfın seçilen haftadaki öğrenci listesi (spec 0014 gereksinim 1): başlıkta sınıf ve hafta aralığı ("5-A · 16–22 Kasım"),
 * öğrenciler sunucunun Türkçe ad sırasıyla, durum metinle ("Özet yok" / "Taslak hazır" / "Gönderildi" — renk tek başına anlam
 * taşımaz). Durumlar: yükleniyor, boş ("Bu sınıfta öğrenci yok"), dolu, hata + tekrar dene.
 */
export function StudentStatusList({ client, classroomId, week, onOpen }: Props) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/weekly-summaries", { params: { query: { classroomId, week } } })).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", list: outcome.data } : { kind: "error", message: outcome.unreachable ? t("weekly.unreachable") : outcome.message ?? t("weekly.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, classroomId, week, reload]);

  if (state.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("weekly.loading")}</p>;
  }

  if (state.kind === "error") {
    return (
      <>
        <p className="form-error" role="alert">{state.message}</p>
        <div className="actions">
          <button type="button" onClick={() => { setState({ kind: "loading" }); setReload((n) => n + 1); }}>{t("weekly.retry")}</button>
        </div>
      </>
    );
  }

  const { list } = state;
  return (
    <>
      <h3>{fillText(t("weekly.listHeading"), { classroom: list.classroomName, week: list.weekLabel })}</h3>
      {list.students.length === 0 ? (
        <p>{t("weekly.noStudents")}</p>
      ) : (
        <ul className="admin-list">
          {list.students.map((student) => {
            const status = t(`weekly.status.${summaryStatusKey(student.status)}` as TextKey);
            return (
              <li key={student.studentId} className="admin-row">
                <span>
                  <strong>{student.fullName}</strong>
                  <span className="notice"> · {status}</span>
                </span>
                <button type="button" className="secondary" aria-label={`${student.fullName}, ${status}, ${t("weekly.open")}`} onClick={() => onOpen(student.studentId)}>
                  {t("weekly.open")}
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </>
  );
}
