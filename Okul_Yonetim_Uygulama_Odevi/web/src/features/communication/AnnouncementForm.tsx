import { useEffect, useRef, useState, type FormEvent } from "react";
import {
  announcementBody,
  ANNOUNCEMENT_SCOPES,
  TEACHER_SCOPES,
  turkeyToday,
  validateAnnouncementForm,
  type AnnouncementFormErrors,
  type AnnouncementFormValues,
  type AnnouncementScope,
  type AuthSession,
  type BranchSummary,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { fieldText, scopeText } from "./communicationText";

type Client = AuthSession["client"];
type Option = { id: string; label: string };

interface Props {
  client: Client;
  /** Kurucu: okul, şube, sınıf, öğrenci; öğretmen yalnızca kendi sınıfları ve öğrencileri. */
  isFounder: boolean;
  branches: readonly BranchSummary[];
  onCancel: () => void;
  onPublished: (id: string) => void;
}

const empty: AnnouncementFormValues = {
  title: "",
  body: "",
  scope: "Classroom",
  scopeId: "",
  requestConsent: false,
  consentQuestion: "",
  consentDeadline: "",
};

/**
 * Yeni duyuru formu (spec 0011 gereksinim 1, 4; AC-12): kapsam (öğretmende yalnızca sınıf/öğrenci), başlık, metin; "Onay
 * iste" seçilince soru ve son gün. Alan hatası varsa istek gönderilmez ve hata alanla ilişkilendirilir; gönderilirken düğme
 * kilitli; sunucu hatası (ör. "Bu kapsamda duyuru alacak veli yok.") duyurulur; yayınlanınca ayrıntıya geçilir. Öğrenci
 * kapsamında önce sınıf, sonra o sınıfın öğrencisi seçilir; seçenekler yayının kabul ettiği kümedir (İletişim modülünün
 * kapsam uçları: sınıfın bütün öğrencileri, sayfasız ve okul gününden bağımsız — review M-1).
 */
export function AnnouncementForm({ client, isFounder, branches, onCancel, onPublished }: Props) {
  const scopes = isFounder ? ANNOUNCEMENT_SCOPES : TEACHER_SCOPES;
  const [values, setValues] = useState<AnnouncementFormValues>(empty);
  const [errors, setErrors] = useState<AnnouncementFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const [classrooms, setClassrooms] = useState<Option[]>([]);
  const [studentClassroom, setStudentClassroom] = useState("");
  const [students, setStudents] = useState<Option[]>([]);
  const firstField = useRef<HTMLSelectElement>(null);

  useEffect(() => {
    firstField.current?.focus();
  }, []);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/announcements/scope-classrooms"))
      .then((o) => (o.ok ? o.data.map((c) => ({ id: c.id, label: `${c.name} · ${c.branchName}` })) : []))
      .then((options) => {
        if (active) {
          setClassrooms(options);
        }
      });
    return () => {
      active = false;
    };
  }, [client]);

  useEffect(() => {
    if (studentClassroom === "") {
      return;
    }

    let active = true;
    call(client.GET("/api/announcements/scope-students", { params: { query: { classroomId: studentClassroom } } }))
      .then((o) => (o.ok ? o.data.map((s) => ({ id: s.id, label: s.fullName })) : []))
      .then((options) => {
        if (active) {
          setStudents(options);
        }
      });
    return () => {
      active = false;
    };
  }, [client, studentClassroom]);

  const set = (next: Partial<AnnouncementFormValues>) => setValues((current) => ({ ...current, ...next }));

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateAnnouncementForm(values, turkeyToday());
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    setServerError(null);
    const outcome = await call(client.POST("/api/announcements", { body: announcementBody(values) }));
    setSaving(false);
    if (outcome.ok) {
      onPublished(outcome.data.id);
    } else {
      setServerError(failureText(outcome));
    }
  };

  const described = (name: keyof AnnouncementFormValues) => (errors[name] ? `announcement-${name}-error` : undefined);
  const invalid = (name: keyof AnnouncementFormValues) => (errors[name] ? true : undefined);
  const error = (name: keyof AnnouncementFormValues) => {
    const found = errors[name];
    return found ? <p id={`announcement-${name}-error`} className="field-error" role="alert">{fieldText(found)}</p> : null;
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={t("communication.new")}>
      <label htmlFor="announcement-scope">{t("communication.scope")}</label>
      <select id="announcement-scope" ref={firstField} value={values.scope}
        onChange={(e) => { set({ scope: e.target.value as AnnouncementScope, scopeId: "" }); setStudentClassroom(""); setStudents([]); }}>
        {scopes.map((s) => <option key={s} value={s}>{scopeText(s)}</option>)}
      </select>

      {values.scope === "Branch" && (
        <>
          <label htmlFor="announcement-scopeId">{t("communication.branch")}</label>
          <select id="announcement-scopeId" value={values.scopeId} onChange={(e) => set({ scopeId: e.target.value })}
            aria-invalid={invalid("scopeId")} aria-describedby={described("scopeId")}>
            <option value="">{t("communication.choose")}</option>
            {branches.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
          </select>
        </>
      )}
      {values.scope === "Classroom" && (
        <>
          <label htmlFor="announcement-scopeId">{t("communication.classroom")}</label>
          <select id="announcement-scopeId" value={values.scopeId} onChange={(e) => set({ scopeId: e.target.value })}
            aria-invalid={invalid("scopeId")} aria-describedby={described("scopeId")}>
            <option value="">{t("communication.choose")}</option>
            {classrooms.map((c) => <option key={c.id} value={c.id}>{c.label}</option>)}
          </select>
        </>
      )}
      {values.scope === "Student" && (
        <>
          <label htmlFor="announcement-studentClassroom">{t("communication.classroom")}</label>
          <select id="announcement-studentClassroom" value={studentClassroom}
            onChange={(e) => { setStudentClassroom(e.target.value); setStudents([]); set({ scopeId: "" }); }}>
            <option value="">{t("communication.choose")}</option>
            {classrooms.map((c) => <option key={c.id} value={c.id}>{c.label}</option>)}
          </select>
          <label htmlFor="announcement-scopeId">{t("communication.student")}</label>
          <select id="announcement-scopeId" value={values.scopeId} onChange={(e) => set({ scopeId: e.target.value })}
            aria-invalid={invalid("scopeId")} aria-describedby={described("scopeId")}>
            <option value="">{t("communication.choose")}</option>
            {students.map((s) => <option key={s.id} value={s.id}>{s.label}</option>)}
          </select>
        </>
      )}
      {error("scopeId")}

      <label htmlFor="announcement-title">{t("communication.titleField")}</label>
      <input id="announcement-title" value={values.title} onChange={(e) => set({ title: e.target.value })}
        aria-invalid={invalid("title")} aria-describedby={described("title")} />
      {error("title")}

      <label htmlFor="announcement-body">{t("communication.body")}</label>
      <textarea id="announcement-body" value={values.body} onChange={(e) => set({ body: e.target.value })}
        aria-invalid={invalid("body")} aria-describedby={described("body")} />
      {error("body")}

      <label className="check">
        <input type="checkbox" checked={values.requestConsent} onChange={(e) => set({ requestConsent: e.target.checked })} />
        {t("communication.requestConsent")}
      </label>
      {values.requestConsent && (
        <>
          <label htmlFor="announcement-consentQuestion">{t("communication.question")}</label>
          <input id="announcement-consentQuestion" value={values.consentQuestion} onChange={(e) => set({ consentQuestion: e.target.value })}
            aria-invalid={invalid("consentQuestion")} aria-describedby={described("consentQuestion")} />
          {error("consentQuestion")}
          <label htmlFor="announcement-consentDeadline">{t("communication.deadline")}</label>
          <input id="announcement-consentDeadline" type="date" value={values.consentDeadline} onChange={(e) => set({ consentDeadline: e.target.value })}
            aria-invalid={invalid("consentDeadline")} aria-describedby={described("consentDeadline")} />
          {error("consentDeadline")}
        </>
      )}

      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("communication.publishing") : t("communication.publish")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("communication.cancel")}</button>
      </div>
    </form>
  );
}
