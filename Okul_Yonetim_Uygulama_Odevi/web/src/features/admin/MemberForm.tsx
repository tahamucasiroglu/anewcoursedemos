import { useState, type FormEvent } from "react";
import { validateMemberForm, type AddMemberResult, type AuthSession, type BranchAdminView, type MemberFormErrors, type MemberFormValues } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "./adminApi";
import { failureText, fieldText } from "./adminText";
import { RolesEditor } from "./RolesEditor";

type Client = AuthSession["client"];
const EMPTY: MemberFormValues = { fullName: "", email: "", phone: "", roles: [] };

interface Props {
  client: Client;
  branches: BranchAdminView[];
  onCancel: () => void;
  onAdded: (result: AddMemberResult) => void;
}

/**
 * Kullanıcı ekle formu (spec 0005): ad-soyad, e-posta, telefon, roller ve şubeler. Alan hataları istek göndermeden
 * gösterilir ve alanla ilişkilidir; gönderilirken düğme kilitli; sunucu hatası (ör. "Bu kişi bu okulda zaten kayıtlı").
 */
export function MemberForm({ client, branches, onCancel, onAdded }: Props) {
  const [values, setValues] = useState(EMPTY);
  const [errors, setErrors] = useState<MemberFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateMemberForm(values);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const outcome = await call(client.POST("/api/admin/members", {
      body: { fullName: values.fullName.trim(), email: values.email.trim(), phone: values.phone.trim() || null, roles: values.roles },
    }));
    setSaving(false);
    if (outcome.ok) {
      onAdded(outcome.data);
    } else {
      setServerError(failureText(outcome));
    }
  };

  const field = (name: "fullName" | "email" | "phone", label: string, type = "text") => {
    const inputId = `member-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={inputId}>{label}</label>
        <input
          id={inputId}
          type={type}
          value={values[name]}
          onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
        {error && <p id={`${inputId}-error`} className="field-error" role="alert">{fieldText(error)}</p>}
      </>
    );
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={t("members.add")}>
      {field("fullName", t("members.fullName"))}
      {field("email", t("members.email"), "email")}
      {field("phone", t("members.phone"), "tel")}
      <RolesEditor idPrefix="new" value={values.roles} branches={branches} onChange={(roles) => setValues({ ...values, roles })} errorId={errors.roles ? "member-roles-error" : undefined} />
      {errors.roles && <p id="member-roles-error" className="field-error" role="alert">{fieldText(errors.roles)}</p>}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("members.saving") : t("members.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("members.cancel")}</button>
      </div>
    </form>
  );
}
