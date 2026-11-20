import { useState, type FormEvent } from "react";
import {
  GUARDIAN_MAX,
  GUARDIAN_RELATIONSHIPS,
  validateGuardianForm,
  type AuthSession,
  type GuardianFormErrors,
  type GuardianFormValues,
  type LinkGuardianResult,
  type StudentGuardianView,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { ConfirmDialog } from "../admin/ConfirmDialog";
import { TempPasswordResult } from "../admin/TempPasswordResult";
import { failureText } from "../admin/adminText";
import { call } from "./studentsApi";
import { relationshipText, studentFieldText } from "./studentsText";

const EMPTY: GuardianFormValues = { fullName: "", email: "", phone: "", relationship: "" };

interface Props {
  client: AuthSession["client"];
  studentId: string;
  guardians: StudentGuardianView[];
  onChanged: () => void;
}

/**
 * Kartın Veliler bölümü (spec 0009 gereksinim 6–7): veli listesi (ad, yakınlık, e-posta, telefon), "Veli ekle" formu
 * (ad-soyad, e-posta, telefon, yakınlık) → sonuç: geçici şifre bir kez (spec 0005 kalıbı) ya da "Bu kişinin zaten bir
 * hesabı var; mevcut şifresiyle girer."; satırda onaylı "Kaldır". 4 velide "Veli ekle" pasif ve nedeni yazılı.
 */
export function GuardiansSection({ client, studentId, guardians, onChanged }: Props) {
  const [adding, setAdding] = useState(false);
  const [values, setValues] = useState(EMPTY);
  const [errors, setErrors] = useState<GuardianFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const [result, setResult] = useState<LinkGuardianResult | null>(null);
  const [removing, setRemoving] = useState<StudentGuardianView | null>(null);

  if (result) {
    return result.temporaryPassword ? (
      <TempPasswordResult password={result.temporaryPassword} onDone={onChanged} />
    ) : (
      <section className="card">
        <p role="status">{t("members.existingAccount")}</p>
        <div className="actions">
          <button type="button" onClick={onChanged}>{t("temp.done")}</button>
        </div>
      </section>
    );
  }

  const full = guardians.length >= GUARDIAN_MAX;
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateGuardianForm(values);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    setServerError(null);
    const outcome = await call(client.POST("/api/admin/students/{id}/guardians", {
      params: { path: { id: studentId } },
      body: { fullName: values.fullName.trim(), email: values.email.trim(), phone: values.phone.trim() || null, relationship: values.relationship },
    }));
    setSaving(false);
    if (outcome.ok) {
      setResult(outcome.data);
    } else {
      setServerError(failureText(outcome));
    }
  };

  const remove = async () => {
    if (!removing) {
      return;
    }

    setSaving(true);
    const outcome = await call(client.DELETE("/api/admin/students/{id}/guardians/{userId}", { params: { path: { id: studentId, userId: removing.userId } } }));
    setSaving(false);
    setRemoving(null);
    if (outcome.ok) {
      onChanged();
    } else {
      setServerError(failureText(outcome));
    }
  };

  const field = (name: "fullName" | "email" | "phone", label: string, type = "text") => {
    const inputId = `guardian-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={inputId}>{label}</label>
        <input id={inputId} type={type} value={values[name]} onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined} aria-describedby={error ? `${inputId}-error` : undefined} />
        {error && <p id={`${inputId}-error`} className="field-error" role="alert">{studentFieldText(error)}</p>}
      </>
    );
  };

  return (
    <div>
      <h4>{t("students.guardians")}</h4>
      {guardians.length === 0 ? (
        <p className="notice">{t("students.noGuardians")}</p>
      ) : (
        <ul className="admin-list">
          {guardians.map((g) => (
            <li key={g.userId} className="admin-row">
              <span>
                <strong>{g.fullName}</strong> <span className="notice">({relationshipText(g.relationship)})</span>
                <br />
                <span className="notice">{[g.email, g.phone].filter(Boolean).join(" · ")}</span>
              </span>
              <button type="button" className="secondary" onClick={() => setRemoving(g)} aria-label={`${g.fullName} — ${t("students.removeGuardian")}`}>
                {t("students.removeGuardian")}
              </button>
            </li>
          ))}
        </ul>
      )}
      {serverError && !adding && <p className="form-error" role="alert">{serverError}</p>}
      {adding ? (
        <form className="auth-form" onSubmit={submit} noValidate aria-label={t("students.addGuardian")}>
          {field("fullName", t("students.fullName"))}
          {field("email", t("students.email"), "email")}
          {field("phone", t("students.phone"), "tel")}
          <label htmlFor="guardian-relationship">{t("students.relationship")}</label>
          <select id="guardian-relationship" value={values.relationship} onChange={(e) => setValues({ ...values, relationship: e.target.value })}
            aria-invalid={errors.relationship ? true : undefined} aria-describedby={errors.relationship ? "guardian-relationship-error" : undefined}>
            <option value="">{t("students.chooseRelationship")}</option>
            {GUARDIAN_RELATIONSHIPS.map((r) => <option key={r} value={r}>{relationshipText(r)}</option>)}
          </select>
          {errors.relationship && <p id="guardian-relationship-error" className="field-error" role="alert">{studentFieldText(errors.relationship)}</p>}
          {serverError && <p className="form-error" role="alert">{serverError}</p>}
          <div className="actions">
            <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("students.saving") : t("students.save")}</button>
            <button type="button" className="secondary" onClick={() => { setAdding(false); setValues(EMPTY); setErrors({}); setServerError(null); }}>{t("students.cancel")}</button>
          </div>
        </form>
      ) : (
        <div className="actions">
          <button type="button" disabled={full} aria-describedby={full ? "guardian-limit" : undefined} onClick={() => { setAdding(true); setServerError(null); }}>
            {t("students.addGuardian")}
          </button>
          {full && <span id="guardian-limit" className="notice">{t("students.guardianLimit")}</span>}
        </div>
      )}
      {removing && <ConfirmDialog message={t("students.confirmRemoveGuardian")} busy={saving} onCancel={() => setRemoving(null)} onConfirm={() => void remove()} />}
    </div>
  );
}
