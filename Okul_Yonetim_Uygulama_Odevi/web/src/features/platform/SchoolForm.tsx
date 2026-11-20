import { useState, type FormEvent } from "react";
import { callApi, validateSchoolForm, type AuthSession, type PanelSchoolResult, type PlatformFieldError, type SchoolFormErrors, type SchoolFormValues } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { failureText } from "../admin/adminText";

interface Props {
  client: AuthSession["client"];
  onCancel: () => void;
  onCreated: (result: PanelSchoolResult) => void;
}

const EMPTY: SchoolFormValues = { schoolName: "", branchName: "", founderName: "", founderEmail: "" };

export function platformFieldText(error: PlatformFieldError): string {
  return t(`platform.field.${error}` as TextKey);
}

/**
 * Okul ekle formu (spec 0007): okul adı, ilk şube, kurucu ad-soyad ve e-posta. Alan hataları istek göndermeden ve alanla
 * ilişkili; gönderilirken düğme kilitli; sunucu hatası ("Bu okul zaten var") duyurulur.
 */
export function SchoolForm({ client, onCancel, onCreated }: Props) {
  const [values, setValues] = useState(EMPTY);
  const [errors, setErrors] = useState<SchoolFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateSchoolForm(values);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    setServerError(null);
    const body = {
      schoolName: values.schoolName.trim(),
      branchName: values.branchName.trim(),
      founderName: values.founderName.trim(),
      founderEmail: values.founderEmail.trim(),
    };
    const outcome = await callApi(client.POST("/api/platform/schools", { body }));
    setSaving(false);
    if (outcome.ok) {
      onCreated(outcome.data);
    } else {
      setServerError(failureText(outcome));
    }
  };

  const field = (name: keyof SchoolFormValues, label: string, type = "text") => {
    const inputId = `school-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={inputId}>{label}</label>
        <input
          id={inputId}
          type={type}
          autoComplete="off"
          value={values[name]}
          onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
        {error && <p id={`${inputId}-error`} className="field-error" role="alert">{platformFieldText(error)}</p>}
      </>
    );
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={t("platform.add")}>
      {field("schoolName", t("platform.schoolName"))}
      {field("branchName", t("platform.branchName"))}
      {field("founderName", t("platform.founderName"))}
      {field("founderEmail", t("platform.founderEmail"), "email")}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("platform.creating") : t("platform.create")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("members.cancel")}</button>
      </div>
    </form>
  );
}
