import { useEffect, useState } from "react";
import {
  callApi,
  canGenerate,
  fillText,
  formatDay,
  TEACHER_NOTE_MAX,
  SUMMARY_TEXT_MAX,
  validateSummaryText,
  validateTeacherNote,
  type AuthSession,
  type SummaryFieldError,
  type WeekChoice,
  type WeeklySummaryPanel,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

type Busy = "generating" | "sending" | "discarding" | null;

type State =
  | { kind: "loading" }
  | { kind: "error"; message: string }
  | { kind: "ready"; panel: WeeklySummaryPanel; note: string; text: string };

interface Props {
  client: AuthSession["client"];
  studentId: string;
  week: WeekChoice;
}

/**
 * Öğrenci-hafta paneli (spec 0014 gereksinim 2–5, 8): dayanak (yoklama sayıları ve günleri, ödevler, notlar, kısa dayanak satırı),
 * isteğe bağlı öğretmen notu (en fazla 500), "Özet oluştur" / "Yeniden oluştur" → "Oluşturuluyor…" (duyurulur, düğmeler kilitli) →
 * "AI taslağı — göndermeden önce kontrol edin" etiketli düzenlenebilir metin (boş olamaz, en fazla 1500) → "Onayla ve gönder" ya da
 * "Vazgeç"; gönderilmişse salt okunur son metin. Dayanak boşsa "Bu hafta için özetlenecek veri yok." ve oluştur ancak not yazılınca
 * açılır. Alan hataları alanla ilişkili ve duyurulur, istek gitmez; sunucu hataları (kullanılamıyor, zaten gönderildi, velisi yok)
 * duyurulur. Panel öğrenci + haftaya bağlı anahtarla kurulur (T15); taslak gelince metin alanı taslakla yeniden doldurulur.
 */
export function SummaryPanel({ client, studentId, week }: Props) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [busy, setBusy] = useState<Busy>(null);
  const [noteError, setNoteError] = useState<SummaryFieldError | null>(null);
  const [textError, setTextError] = useState<SummaryFieldError | null>(null);
  const [problem, setProblem] = useState<string | null>(null);
  const [result, setResult] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/weekly-summaries/students/{studentId}", { params: { path: { studentId }, query: { week } } })).then((outcome) => {
      if (active) {
        setState(outcome.ok ? ready(outcome.data) : { kind: "error", message: outcome.unreachable ? t("weekly.unreachable") : outcome.message ?? t("weekly.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, studentId, week, reload]);

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

  const { panel, note, text } = state;
  const { attendance, assignments, grades } = panel.basis;
  const failure = (outcome: { unreachable: boolean; message?: string | null }) =>
    outcome.unreachable ? t("weekly.unreachable") : outcome.message ?? t("weekly.actionError");

  const generate = async () => {
    const error = validateTeacherNote(note);
    setNoteError(error);
    setResult(null);
    setProblem(null);
    if (error) {
      return;
    }

    setBusy("generating");
    const outcome = await callApi(client.POST("/api/weekly-summaries/students/{studentId}/draft", {
      params: { path: { studentId }, query: { week } },
      body: { teacherNote: note.trim() === "" ? null : note },
    }));
    setBusy(null);
    if (outcome.ok) {
      setState(ready(outcome.data));
      setTextError(null);
    } else {
      setProblem(failure(outcome));
    }
  };

  const send = async () => {
    const error = validateSummaryText(text);
    setTextError(error);
    setResult(null);
    setProblem(null);
    if (error) {
      return;
    }

    setBusy("sending");
    const outcome = await callApi(client.POST("/api/weekly-summaries/students/{studentId}/send", { params: { path: { studentId }, query: { week } }, body: { text } }));
    setBusy(null);
    if (outcome.ok) {
      setResult(fillText(t("weekly.sentResult"), { n: outcome.data.notifiedGuardians }));
      setReload((n) => n + 1);
    } else {
      setProblem(failure(outcome));
    }
  };

  const discard = async () => {
    setResult(null);
    setProblem(null);
    setBusy("discarding");
    const outcome = await callApi(client.DELETE("/api/weekly-summaries/students/{studentId}/draft", { params: { path: { studentId }, query: { week } } }));
    setBusy(null);
    if (outcome.ok) {
      setResult(t("weekly.discarded"));
      setReload((n) => n + 1);
    } else {
      setProblem(failure(outcome));
    }
  };

  const locked = busy !== null;
  const sent = panel.status === "Sent";
  const hasDraft = panel.draftId !== null && panel.draftId !== undefined;
  return (
    <div className="weekly-panel">
      <h3>{fillText(t("weekly.studentWeek"), { name: panel.fullName, week: panel.weekLabel })}</h3>
      <section aria-labelledby="weekly-basis-title">
        <h4 id="weekly-basis-title">{t("weekly.basis")}</h4>
        <p>
          {attendance.daysTaken === 0
            ? t("weekly.noAttendance")
            : fillText(t("weekly.attendance"), { days: attendance.daysTaken, present: attendance.present, late: attendance.late, absent: attendance.absent })}
        </p>
        {attendance.absentDays.length > 0 && <p>{fillText(t("weekly.absentDays"), { days: attendance.absentDays.map(formatDay).join(", ") })}</p>}
        {attendance.lateDays.length > 0 && <p>{fillText(t("weekly.lateDays"), { days: attendance.lateDays.map(formatDay).join(", ") })}</p>}
        {assignments.length > 0 && (
          <>
            <h5>{t("weekly.assignments")}</h5>
            <ul>
              {assignments.map((a) => (
                <li key={a.assignmentId}>
                  {a.subject} — {a.title} ({fillText(t("weekly.due"), { date: formatDay(a.dueDate) })}): {t(`weekly.submission.${a.status}` as TextKey)}
                </li>
              ))}
            </ul>
          </>
        )}
        {grades.length > 0 && (
          <>
            <h5>{t("weekly.grades")}</h5>
            <ul>
              {grades.map((g) => (
                <li key={g.assessmentId}>
                  {g.subject} {t(`weekly.gradeKind.${g.kind}` as TextKey)} — {g.title} ({formatDay(g.date)}): {g.score === null || g.score === undefined ? t("weekly.noScore") : g.score}
                </li>
              ))}
            </ul>
          </>
        )}
        <p className="notice">{panel.basisLine}</p>
      </section>

      {sent ? (
        <section aria-labelledby="weekly-sent-title">
          <h4 id="weekly-sent-title">{t("weekly.sent")}</h4>
          <p className="weekly-final">{panel.finalText}</p>
        </section>
      ) : (
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            if (!locked && canGenerate(panel.hasData, note)) {
              void generate();
            }
          }}
        >
          {!panel.hasData && <p role="status">{t("weekly.noData")}</p>}
          <label htmlFor="weekly-note">{fillText(t("weekly.note"), { max: TEACHER_NOTE_MAX })}</label>
          <textarea
            id="weekly-note"
            value={note}
            rows={3}
            disabled={locked}
            aria-invalid={noteError !== null}
            aria-describedby={noteError ? "weekly-note-error" : undefined}
            onChange={(event) => setState({ ...state, note: event.target.value })}
          />
          {noteError && <p id="weekly-note-error" className="form-error" role="alert">{t("weekly.noteTooLong")}</p>}
          <div className="actions">
            <button type="submit" disabled={locked || !canGenerate(panel.hasData, note)} aria-busy={busy === "generating"}>
              {busy === "generating" ? t("weekly.generating") : hasDraft ? t("weekly.regenerate") : t("weekly.generate")}
            </button>
          </div>
        </form>
      )}
      {busy === "generating" && <p role="status">{t("weekly.generating")}</p>}

      {!sent && hasDraft && (
        <form
          noValidate
          onSubmit={(event) => {
            event.preventDefault();
            if (!locked) {
              void send();
            }
          }}
        >
          <p className="weekly-ai-label">{t("weekly.draftLabel")}</p>
          <label htmlFor="weekly-text">{fillText(t("weekly.text"), { max: SUMMARY_TEXT_MAX })}</label>
          <textarea
            id="weekly-text"
            value={text}
            rows={6}
            disabled={locked}
            aria-invalid={textError !== null}
            aria-describedby={textError ? "weekly-text-error" : undefined}
            onChange={(event) => setState({ ...state, text: event.target.value })}
          />
          {textError && <p id="weekly-text-error" className="form-error" role="alert">{t(textError === "required" ? "weekly.textRequired" : "weekly.textTooLong")}</p>}
          <div className="actions">
            <button type="submit" disabled={locked} aria-busy={busy === "sending"}>{busy === "sending" ? t("weekly.sending") : t("weekly.send")}</button>
            <button type="button" className="secondary" disabled={locked} onClick={() => void discard()}>{t("weekly.discard")}</button>
          </div>
        </form>
      )}
      {problem && <p className="form-error" role="alert">{problem}</p>}
      {result && <p role="status">{result}</p>}
    </div>
  );
}

function ready(panel: WeeklySummaryPanel): State {
  return { kind: "ready", panel, note: panel.basis.teacherNote ?? "", text: panel.draftText ?? "" };
}
