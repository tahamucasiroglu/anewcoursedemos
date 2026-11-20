import { useEffect, useState, type FormEvent } from "react";
import { callApi, createOkulApiClient, validateNewPassword, type NewPasswordErrors } from "@okul/api-client";
import { apiBaseUrl } from "../../config";
import { t, type TextKey } from "../../i18n/t";

interface Props {
  token: string;
  onDone: () => void;
}

type State = { kind: "checking" } | { kind: "invalid" } | { kind: "form"; saving: boolean; message: string | null } | { kind: "done" } | { kind: "unreachable" };

/**
 * Yeni şifre belirle (spec 0006, web): e-postadaki bağlantı açar. Önce bağlantı denetlenir; geçersiz/süresi dolmuşsa
 * "Yeni bağlantı iste"; geçerliyse yeni şifre + tekrar. Başarıda bütün oturumlar kapanır, "Girişe git".
 */
export function ResetPasswordScreen({ token, onDone }: Props) {
  const [client] = useState(() => createOkulApiClient(apiBaseUrl));
  const [state, setState] = useState<State>(() => (token === "" ? { kind: "invalid" } : { kind: "checking" }));
  const [attempt, setAttempt] = useState(0);
  const [values, setValues] = useState({ next: "", repeat: "" });
  const [errors, setErrors] = useState<NewPasswordErrors>({});

  useEffect(() => {
    let active = true;
    if (token === "") {
      return;
    }

    callApi(client.POST("/api/auth/reset-password/check", { body: { token } })).then((outcome) => {
      if (active) {
        setState(outcome.ok ? (outcome.data.valid ? { kind: "form", saving: false, message: null } : { kind: "invalid" }) : outcome.unreachable ? { kind: "unreachable" } : { kind: "invalid" });
      }
    });
    return () => {
      active = false;
    };
  }, [client, token, attempt]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateNewPassword(values.next, values.repeat);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setState({ kind: "form", saving: true, message: null });
    const outcome = await callApi(client.POST("/api/auth/reset-password", { body: { token, newPassword: values.next, newPasswordRepeat: values.repeat } }));
    if (outcome.ok) {
      setState({ kind: "done" });
    } else if (!outcome.unreachable && outcome.message === t("account.linkInvalid")) {
      setState({ kind: "invalid" });
    } else {
      setState({ kind: "form", saving: false, message: outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("account.loadError") });
    }
  };

  const heading = <h2 id="reset-title">{t("account.resetTitle")}</h2>;
  switch (state.kind) {
    case "checking":
      return <section className="card auth-form" aria-labelledby="reset-title">{heading}<p className="notice" aria-busy="true">{t("account.checkingLink")}</p></section>;
    case "unreachable":
      return (
        <section className="card auth-form" aria-labelledby="reset-title">
          {heading}
          <p className="form-error" role="alert">{t("auth.unreachable")}</p>
          <div className="actions"><button type="button" onClick={() => { setState({ kind: "checking" }); setAttempt((n) => n + 1); }}>{t("auth.retry")}</button></div>
        </section>
      );
    case "invalid":
      return (
        <section className="card auth-form" aria-labelledby="reset-title">
          {heading}
          <p className="form-error" role="alert">{t("account.linkInvalid")}</p>
          <div className="actions"><a className="button-link" href="#/sifremi-unuttum">{t("account.requestNewLink")}</a></div>
        </section>
      );
    case "done":
      return (
        <section className="card auth-form" aria-labelledby="reset-title">
          {heading}
          <p role="status">{t("account.resetDone")}</p>
          <div className="actions"><button type="button" onClick={onDone}>{t("account.goToLogin")}</button></div>
        </section>
      );
    case "form": {
      const field = (name: "next" | "repeat", label: string) => {
        const id = `reset-${name}`;
        const error = errors[name];
        return (
          <>
            <label htmlFor={id}>{label}</label>
            <input id={id} type="password" autoComplete="new-password" value={values[name]} onChange={(e) => setValues({ ...values, [name]: e.target.value })}
              aria-invalid={error ? true : undefined} aria-describedby={error ? `${id}-error` : undefined} />
            {error && <p id={`${id}-error`} className="field-error" role="alert">{t(`account.field.${error}` as TextKey)}</p>}
          </>
        );
      };
      return (
        <form className="card auth-form" onSubmit={submit} noValidate aria-labelledby="reset-title">
          {heading}
          {field("next", t("account.newPassword"))}
          {field("repeat", t("account.newPasswordRepeat"))}
          {state.message && <p className="form-error" role="alert">{state.message}</p>}
          <div className="actions">
            <button type="submit" disabled={state.saving} aria-busy={state.saving}>{state.saving ? t("account.changing") : t("account.resetSubmit")}</button>
          </div>
        </form>
      );
    }
  }
}
