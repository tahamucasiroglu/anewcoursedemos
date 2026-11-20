import { useEffect, useState, type FormEvent } from "react";
import { formatDay, validateHealthForm, type AuthSession, type HealthFormValues, type StudentDetail } from "@okul/api-client";
import { t } from "../../i18n/t";
import { ConfirmDialog } from "../admin/ConfirmDialog";
import { failureText } from "../admin/adminText";
import { GuardiansSection } from "./GuardiansSection";
import { PickupPersonsSection } from "./PickupPersonsSection";
import { StudentForm } from "./StudentForm";
import { call } from "./studentsApi";
import { studentFieldText } from "./studentsText";

type Client = AuthSession["client"];
type CardState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; student: StudentDetail };

interface Props {
  client: Client;
  studentId: string;
  onBack: () => void;
  onOpen: (id: string) => void;
}

/**
 * Öğrenci kartı (spec 0009): Bilgiler (düzenle), Sağlık (sağlık notu ve alerji ayrı başlıklarla; anahtar yoksa "Sağlık
 * bilgisi şu anda gösterilemiyor."), Veliler, Kardeşler (satır o kardeşin kartını açar), Teslim alabilecek kişiler ve
 * "Ayrıldı olarak işaretle" (onaylı) / "Geri al". Boş bölümler kendi mesajını yazar; ayrılan öğrencide "Ayrıldı" etiketi.
 */
export function StudentCard({ client, studentId, onBack, onOpen }: Props) {
  const [state, setState] = useState<CardState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [mode, setMode] = useState<"view" | "editInfo" | "editHealth">("view");
  const [health, setHealth] = useState<HealthFormValues>({ healthNote: "", allergy: "" });
  const [healthErrors, setHealthErrors] = useState<ReturnType<typeof validateHealthForm>>({});
  const [confirmLeave, setConfirmLeave] = useState(false);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<{ error: boolean; text: string } | null>(null);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/admin/students/{id}", { params: { path: { id: studentId } } })).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", student: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, studentId, reload]);

  const refresh = (text?: string) => {
    setMode("view");
    setState({ kind: "loading" });
    setMessage(text ? { error: false, text } : null);
    setReload((n) => n + 1);
  };

  const fail = (outcome: Parameters<typeof failureText>[0]) => setMessage({ error: true, text: failureText(outcome) });

  if (state.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("admin.loading")}</p>;
  }

  if (state.kind === "error") {
    return (
      <div className="card">
        <p className="form-error" role="alert">{state.message}</p>
        <div className="actions">
          <button type="button" onClick={() => refresh()}>{t("admin.retry")}</button>
          <button type="button" className="secondary" onClick={onBack}>{t("students.backToList")}</button>
        </div>
      </div>
    );
  }

  const student = state.student;
  if (mode === "editInfo") {
    return <StudentForm key={`edit-${student.id}`} client={client} student={student} onCancel={() => setMode("view")} onSaved={() => refresh(t("students.saved"))} />;
  }

  const saveHealth = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateHealthForm(health);
    setHealthErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setBusy(true);
    const outcome = await call(client.PUT("/api/admin/students/{id}/health", {
      params: { path: { id: student.id } },
      body: { healthNote: health.healthNote.trim() || null, allergy: health.allergy.trim() || null },
    }));
    setBusy(false);
    if (outcome.ok) {
      refresh(t("students.saved"));
    } else {
      fail(outcome);
    }
  };

  const setActive = async (active: boolean) => {
    setBusy(true);
    const path = { params: { path: { id: student.id } } };
    const outcome = await call(active ? client.POST("/api/admin/students/{id}/activate", path) : client.POST("/api/admin/students/{id}/deactivate", path));
    setBusy(false);
    setConfirmLeave(false);
    if (outcome.ok) {
      refresh(t("students.saved"));
    } else {
      fail(outcome);
    }
  };

  const startHealthEdit = () => {
    setHealth({ healthNote: student.health.healthNote ?? "", allergy: student.health.allergy ?? "" });
    setHealthErrors({});
    setMode("editHealth");
  };

  return (
    <section className="card" aria-labelledby="student-title">
      <div className="actions">
        <button type="button" className="secondary" onClick={onBack}>{t("students.backToList")}</button>
      </div>
      <h3 id="student-title">
        {student.fullName} {!student.isActive && <span className="badge">{t("students.leftBadge")}</span>}
      </h3>
      {message && <p className={message.error ? "form-error" : "notice"} role={message.error ? "alert" : "status"}>{message.text}</p>}

      <h4>{t("students.info")}</h4>
      <dl className="facts">
        <dt>{t("students.birthDate")}</dt>
        <dd>{formatDay(student.birthDate)}</dd>
        <dt>{t("students.schoolNumber")}</dt>
        <dd>{student.schoolNumber ?? "—"}</dd>
        <dt>{t("students.branch")}</dt>
        <dd>{student.branchName}</dd>
        <dt>{t("students.classroom")}</dt>
        <dd>{student.classroomName ?? t("students.noClassroom")}</dd>
      </dl>
      <div className="actions">
        <button type="button" className="secondary" onClick={() => setMode("editInfo")}>{t("students.edit")}</button>
      </div>

      <h4>{t("students.health")}</h4>
      {!student.health.available ? (
        <p className="notice">{t("students.healthUnavailable")}</p>
      ) : mode === "editHealth" ? (
        <form className="auth-form" onSubmit={saveHealth} noValidate aria-label={t("students.health")}>
          <label htmlFor="health-note">{t("students.healthNote")}</label>
          <textarea id="health-note" value={health.healthNote} onChange={(e) => setHealth({ ...health, healthNote: e.target.value })}
            aria-invalid={healthErrors.healthNote ? true : undefined} aria-describedby={healthErrors.healthNote ? "health-note-error" : undefined} />
          {healthErrors.healthNote && <p id="health-note-error" className="field-error" role="alert">{studentFieldText(healthErrors.healthNote)}</p>}
          <label htmlFor="health-allergy">{t("students.allergy")}</label>
          <textarea id="health-allergy" value={health.allergy} onChange={(e) => setHealth({ ...health, allergy: e.target.value })}
            aria-invalid={healthErrors.allergy ? true : undefined} aria-describedby={healthErrors.allergy ? "health-allergy-error" : undefined} />
          {healthErrors.allergy && <p id="health-allergy-error" className="field-error" role="alert">{studentFieldText(healthErrors.allergy)}</p>}
          <div className="actions">
            <button type="submit" disabled={busy} aria-busy={busy}>{busy ? t("students.saving") : t("students.save")}</button>
            <button type="button" className="secondary" onClick={() => setMode("view")}>{t("students.cancel")}</button>
          </div>
        </form>
      ) : (
        <>
          <dl className="facts">
            <dt>{t("students.healthNote")}</dt>
            <dd>{student.health.healthNote ?? t("students.healthEmpty")}</dd>
            <dt>{t("students.allergy")}</dt>
            <dd>{student.health.allergy ?? t("students.healthEmpty")}</dd>
          </dl>
          <div className="actions">
            <button type="button" className="secondary" onClick={startHealthEdit}>{t("students.edit")}</button>
          </div>
        </>
      )}

      <GuardiansSection client={client} studentId={student.id} guardians={student.guardians} onChanged={() => refresh(t("students.saved"))} />

      <h4>{t("students.siblings")}</h4>
      {student.siblings.length === 0 ? (
        <p className="notice">{t("students.noSiblings")}</p>
      ) : (
        <ul className="admin-list">
          {student.siblings.map((s) => (
            <li key={s.id} className="admin-row">
              <span>
                <strong>{s.fullName}</strong> {!s.isActive && <span className="badge">{t("students.leftBadge")}</span>}
                <br />
                <span className="notice">{`${s.branchName} · ${s.classroomName ?? t("students.noClassroom")}`}</span>
              </span>
              <button type="button" className="secondary" onClick={() => onOpen(s.id)} aria-label={`${s.fullName} — ${t("students.open")}`}>{t("students.open")}</button>
            </li>
          ))}
        </ul>
      )}

      <PickupPersonsSection key={`pickup-${student.id}`} client={client} studentId={student.id} persons={student.pickupPersons} onChanged={() => refresh(t("students.saved"))} />

      <div className="actions">
        {student.isActive ? (
          <button type="button" className="secondary" disabled={busy} onClick={() => setConfirmLeave(true)}>{t("students.markLeft")}</button>
        ) : (
          <button type="button" className="secondary" disabled={busy} onClick={() => void setActive(true)}>{t("students.reinstate")}</button>
        )}
      </div>
      {confirmLeave && <ConfirmDialog message={t("students.confirmMarkLeft")} busy={busy} onCancel={() => setConfirmLeave(false)} onConfirm={() => void setActive(false)} />}
    </section>
  );
}
