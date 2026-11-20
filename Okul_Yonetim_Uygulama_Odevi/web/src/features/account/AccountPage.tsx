import { useEffect, useState, type FormEvent } from "react";
import {
  callApi,
  validatePasswordChange,
  validateProfileForm,
  type AccountFieldError,
  type AuthSession,
  type MyProfile,
  type PasswordChangeErrors,
  type PasswordChangeValues,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

type Client = AuthSession["client"];
const fieldText = (error: AccountFieldError) => t(`account.field.${error}` as TextKey);
const failure = (outcome: { unreachable: true } | { unreachable: false; message: string | null }) =>
  outcome.unreachable ? t("account.unreachable") : outcome.message ?? t("account.loadError");

interface Props {
  client: Client;
  onProfileSaved: () => void;
}

/**
 * Hesabım (spec 0006, web): profil (ad-soyad, telefon; e-posta salt okunur) ve şifremi değiştir. Alan hataları istek
 * göndermeden ve alanla ilişkili; gönderilirken düğme kilitli; başarı ve hatalar duyurulur. Şifre alanları parola
 * yöneticisine uygun (autocomplete).
 */
export function AccountPage({ client, onProfileSaved }: Props) {
  const [profile, setProfile] = useState<{ kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; data: MyProfile }>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/account/profile")).then((outcome) => {
      if (active) {
        setProfile(outcome.ok ? { kind: "ready", data: outcome.data } : { kind: "error", message: failure(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  return (
    <section className="card admin" aria-labelledby="account-title">
      <div className="admin-head">
        <h2 id="account-title">{t("account.title")}</h2>
        <a href="#/">{t("admin.back")}</a>
      </div>
      {profile.kind === "loading" && <p className="notice" aria-busy="true">{t("admin.loading")}</p>}
      {profile.kind === "error" && (
        <div>
          <p className="form-error" role="alert">{profile.message}</p>
          <div className="actions">
            <button type="button" onClick={() => { setProfile({ kind: "loading" }); setReload((n) => n + 1); }}>{t("admin.retry")}</button>
          </div>
        </div>
      )}
      {profile.kind === "ready" && <ProfileForm client={client} initial={profile.data} onSaved={onProfileSaved} />}
      <ChangePasswordForm client={client} />
    </section>
  );
}

function ProfileForm({ client, initial, onSaved }: { client: Client; initial: MyProfile; onSaved: () => void }) {
  const [values, setValues] = useState({ fullName: initial.fullName, phone: initial.phone ?? "" });
  const [errors, setErrors] = useState<ReturnType<typeof validateProfileForm>>({});
  const [saving, setSaving] = useState(false);
  const [result, setResult] = useState<{ error: boolean; text: string } | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateProfileForm(values);
    setErrors(found);
    setResult(null);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const outcome = await callApi(client.PUT("/api/account/profile", { body: { fullName: values.fullName.trim(), phone: values.phone.trim() || null } }));
    setSaving(false);
    if (outcome.ok) {
      setValues({ fullName: outcome.data.fullName, phone: outcome.data.phone ?? "" });
      setResult({ error: false, text: t("account.profileSaved") });
      onSaved();
    } else {
      setResult({ error: true, text: failure(outcome) });
    }
  };

  const field = (name: "fullName" | "phone", label: string, type: string, autoComplete: string) => {
    const id = `profile-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={id}>{label}</label>
        <input id={id} type={type} autoComplete={autoComplete} value={values[name]} onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined} aria-describedby={error ? `${id}-error` : undefined} />
        {error && <p id={`${id}-error`} className="field-error" role="alert">{fieldText(error)}</p>}
      </>
    );
  };

  return (
    <form className="auth-form" onSubmit={submit} noValidate aria-labelledby="profile-title">
      <h3 id="profile-title">{t("account.profile")}</h3>
      {field("fullName", t("members.fullName"), "text", "name")}
      <label htmlFor="profile-email">{t("members.email")}</label>
      <input id="profile-email" type="email" value={initial.email} readOnly aria-describedby="profile-email-note" />
      <p id="profile-email-note" className="notice">{t("account.emailReadonly")}</p>
      {field("phone", t("members.phone"), "tel", "tel")}
      <p role={result?.error ? "alert" : "status"} className={result?.error ? "form-error" : "notice"}>{result?.text ?? ""}</p>
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("members.saving") : t("members.save")}</button>
      </div>
    </form>
  );
}

const EMPTY: PasswordChangeValues = { current: "", next: "", repeat: "" };

function ChangePasswordForm({ client }: { client: Client }) {
  const [values, setValues] = useState(EMPTY);
  const [errors, setErrors] = useState<PasswordChangeErrors>({});
  const [saving, setSaving] = useState(false);
  const [result, setResult] = useState<{ error: boolean; text: string } | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validatePasswordChange(values);
    setErrors(found);
    setResult(null);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const outcome = await callApi(client.POST("/api/account/change-password", {
      body: { currentPassword: values.current, newPassword: values.next, newPasswordRepeat: values.repeat },
    }));
    setSaving(false);
    if (outcome.ok) {
      setValues(EMPTY);
      setResult({ error: false, text: t("account.passwordChanged") });
    } else {
      setResult({ error: true, text: failure(outcome) });
    }
  };

  const field = (name: keyof PasswordChangeValues, label: string, autoComplete: string) => {
    const id = `password-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={id}>{label}</label>
        <input id={id} type="password" autoComplete={autoComplete} value={values[name]} onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined} aria-describedby={error ? `${id}-error` : undefined} />
        {error && <p id={`${id}-error`} className="field-error" role="alert">{fieldText(error)}</p>}
      </>
    );
  };

  return (
    <form className="auth-form" onSubmit={submit} noValidate aria-labelledby="password-title">
      <h3 id="password-title">{t("account.changePassword")}</h3>
      {field("current", t("account.currentPassword"), "current-password")}
      {field("next", t("account.newPassword"), "new-password")}
      {field("repeat", t("account.newPasswordRepeat"), "new-password")}
      <p role={result?.error ? "alert" : "status"} className={result?.error ? "form-error" : "notice"}>{result?.text ?? ""}</p>
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("account.changing") : t("account.changeSubmit")}</button>
      </div>
    </form>
  );
}
