import { useEffect, useState } from "react";
import {
  ATTENDANCE_STATUSES,
  callApi,
  countMarks,
  fillText,
  initialMarks,
  isStaleSave,
  saveBody,
  saveButtonText,
  saveResultText,
  setMark,
  type AttendanceMarks,
  type AttendanceSheetView,
  type AuthSession,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

type State =
  | { kind: "loading" }
  | { kind: "error"; message: string; forbidden: boolean }
  | { kind: "ready"; sheet: AttendanceSheetView; marks: AttendanceMarks; saving: boolean; result: string | null; saveError: string | null };

interface Props {
  client: AuthSession["client"];
  classroomId: string;
}

/**
 * Bir sınıfın bugünkü yoklaması (spec 0010, web): öğrenciler Türkçe ad sırasıyla, her birinde Var / Yok / Geç (yerel radio
 * — klavyeyle ok tuşları, etiket "Ayşe Yılmaz: Yok"); düğmede sayılar ("Kaydet (3 yok, 1 geç)"), onay iletişimi yok.
 * Durumlar: yükleniyor, erişim yok, okul günü değil, öğrenci yok (kaydet kapalı), kaydedildi (saat), kaydediliyor (düğme
 * kilitli), hata (işaretler korunur, tekrar Kaydet). Sonuç ve hatalar duyurulur. Ekran açıldıktan sonra başkası kaydettiyse
 * (409) liste yeniden okunur ve sunucunun mesajı gösterilir (0010 review F-1).
 */
export function AttendanceList({ client, classroomId }: Props) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/attendance/classrooms/{id}/today", { params: { path: { id: classroomId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", sheet: outcome.data, marks: initialMarks(outcome.data.students), saving: false, result: null, saveError: null });
      } else {
        const forbidden = !outcome.unreachable && (outcome.status === 403 || outcome.status === 404);
        const message = forbidden ? t("attendance.forbidden") : outcome.unreachable ? t("attendance.unreachable") : outcome.message ?? t("attendance.loadError");
        setState({ kind: "error", message, forbidden });
      }
    });
    return () => {
      active = false;
    };
  }, [client, classroomId, reload]);

  if (state.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("attendance.loading")}</p>;
  }

  if (state.kind === "error") {
    return (
      <>
        <p className="form-error" role="alert">{state.message}</p>
        {!state.forbidden && (
          <div className="actions">
            <button type="button" onClick={() => { setState({ kind: "loading" }); setReload((n) => n + 1); }}>{t("attendance.retry")}</button>
          </div>
        )}
      </>
    );
  }

  const { sheet, marks, saving, result, saveError } = state;
  const save = async () => {
    setState({ ...state, saving: true, result: null, saveError: null });
    const outcome = await callApi(client.PUT("/api/attendance/classrooms/{id}/today", { params: { path: { id: classroomId } }, body: saveBody(sheet.day, marks, sheet.version) }));
    if (isStaleSave(outcome)) {
      const notice = (!outcome.ok && !outcome.unreachable && outcome.message) || t("attendance.stale");
      const fresh = await callApi(client.GET("/api/attendance/classrooms/{id}/today", { params: { path: { id: classroomId } } }));
      setState(
        fresh.ok
          ? { kind: "ready", sheet: fresh.data, marks: initialMarks(fresh.data.students), saving: false, result: null, saveError: notice }
          : { kind: "error", message: fresh.unreachable ? t("attendance.unreachable") : fresh.message ?? t("attendance.loadError"), forbidden: false },
      );
      return;
    }

    setState((s) => {
      if (s.kind !== "ready") {
        return s;
      }

      if (outcome.ok) {
        const text = saveResultText(outcome.data, { saved: t("attendance.result"), withoutGuardian: t("attendance.withoutGuardian") });
        return { ...s, saving: false, result: text, sheet: { ...s.sheet, isSaved: true, savedTime: outcome.data.savedTime, version: outcome.data.version } };
      }

      return { ...s, saving: false, saveError: outcome.unreachable ? t("attendance.unreachable") : outcome.message ?? t("attendance.saveError") };
    });
  };

  if (!sheet.isSchoolDay) {
    return (
      <>
        <h3>{sheet.classroomName} · {sheet.branchName}</h3>
        <p role="status">{t("attendance.notSchoolDay")}</p>
      </>
    );
  }

  const empty = sheet.students.length === 0;
  const label = saving
    ? t("attendance.saving")
    : saveButtonText(countMarks(marks), {
        plain: t("attendance.save"),
        withCounts: t("attendance.saveWith"),
        absent: t("attendance.saveAbsent"),
        late: t("attendance.saveLate"),
        separator: t("attendance.saveSeparator"),
      });
  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        if (!saving && !empty) {
          void save();
        }
      }}
    >
      <h3>{sheet.classroomName} · {sheet.branchName}</h3>
      <p className="notice">{sheet.isSaved && sheet.savedTime ? fillText(t("attendance.savedAt"), { time: sheet.savedTime }) : t("attendance.notSaved")}</p>
      {empty ? (
        <p>{t("attendance.noStudents")}</p>
      ) : (
        <ul className="attendance-list">
          {sheet.students.map((student) => (
            <li key={student.studentId}>
              <fieldset className="attendance-row" disabled={saving}>
                <legend>{student.fullName}</legend>
                <span className="attendance-choices">
                  {ATTENDANCE_STATUSES.map((status) => {
                    const text = t(`attendance.status.${status}` as TextKey);
                    const checked = (marks[student.studentId] ?? "Present") === status;
                    return (
                      <label key={status}>
                        <input
                          type="radio"
                          name={`attendance-${student.studentId}`}
                          value={status}
                          checked={checked}
                          aria-label={`${student.fullName}: ${text}`}
                          onChange={() => setState((s) => (s.kind === "ready" ? { ...s, marks: setMark(s.marks, student.studentId, status), result: null } : s))}
                        />
                        {checked ? `✓ ${text}` : text}
                      </label>
                    );
                  })}
                </span>
              </fieldset>
            </li>
          ))}
        </ul>
      )}
      {result && <p role="status">{result}</p>}
      {saveError && <p className="form-error" role="alert">{saveError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving || empty} aria-busy={saving}>{label}</button>
      </div>
    </form>
  );
}
