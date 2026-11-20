import { useState, type FormEvent } from "react";
import { callApi, createOkulApiClient, validateEmailField, type AccountFieldError } from "@okul/api-client";
import { apiBaseUrl } from "../../config";
import { t, type TextKey } from "../../i18n/t";

/**
 * Şifremi unuttum (spec 0006, web): e-posta → her durumda aynı bilgi mesajı (hesabın varlığı ele verilmez). Alan
 * hatasında istek gönderilmez; gönderilirken düğme kilitli; sunucuya ulaşılamazsa tekrar dene.
 */
export function ForgotPasswordScreen() {
  const [client] = useState(() => createOkulApiClient(apiBaseUrl));
  const [email, setEmail] = useState("");
  const [error, setError] = useState<AccountFieldError | null>(null);
  const [state, setState] = useState<{ kind: "form" } | { kind: "sending" } | { kind: "sent"; message: string } | { kind: "failed"; message: string }>({ kind: "form" });

  const submit = async (event?: FormEvent) => {
    event?.preventDefault();
    const found = validateEmailField(email);
    setError(found);
    if (found) {
      return;
    }

    setState({ kind: "sending" });
    const outcome = await callApi(client.POST("/api/auth/forgot-password", { body: { email: email.trim() } }));
    if (outcome.ok) {
      setState({ kind: "sent", message: outcome.data.message });
    } else {
      setState({ kind: "failed", message: outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("account.loadError") });
    }
  };

  if (state.kind === "sent") {
    return (
      <section className="card auth-form" aria-labelledby="forgot-title">
        <h2 id="forgot-title">{t("account.forgotTitle")}</h2>
        <p role="status">{state.message}</p>
        <div className="actions">
          <a className="button-link" href="#/">{t("account.backToLogin")}</a>
        </div>
      </section>
    );
  }

  const sending = state.kind === "sending";
  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-labelledby="forgot-title">
      <h2 id="forgot-title">{t("account.forgotTitle")}</h2>
      <p className="notice">{t("account.forgotIntro")}</p>
      <label htmlFor="forgot-email">{t("auth.email")}</label>
      <input id="forgot-email" type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)}
        aria-invalid={error ? true : undefined} aria-describedby={error ? "forgot-email-error" : undefined} />
      {error && <p id="forgot-email-error" className="field-error" role="alert">{t(`account.field.${error}` as TextKey)}</p>}
      {state.kind === "failed" && <p className="form-error" role="alert">{state.message}</p>}
      <div className="actions">
        <button type="submit" disabled={sending} aria-busy={sending}>{sending ? t("account.sending") : t("account.forgotSubmit")}</button>
        {state.kind === "failed" && <button type="button" className="secondary" onClick={() => void submit()}>{t("auth.retry")}</button>}
        <a href="#/">{t("account.backToLogin")}</a>
      </div>
    </form>
  );
}
