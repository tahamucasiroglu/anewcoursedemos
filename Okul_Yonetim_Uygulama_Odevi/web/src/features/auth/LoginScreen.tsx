import { useState, type FormEvent } from "react";
import { validateLoginForm, type LoginFormErrors, type LoginProblem } from "@okul/api-client";
import { t } from "../../i18n/t";

interface Props {
  submitting: boolean;
  expired: boolean;
  problem: LoginProblem | null;
  onSubmit: (email: string, password: string) => void;
  onRetryResume: () => void;
}

/**
 * Giriş ekranı (spec 0004): e-posta + şifre. Alan hataları istek gönderilmeden gösterilir ve alanla ilişkilidir;
 * sunucu hatası (hatalı bilgi, kilit, hız sınırı) ve "ulaşılamıyor" ekran okuyucuya duyurulur (role=alert).
 * Gönderilirken düğme kilitlidir. "Tekrar dene" aynı bilgilerle yeniden gönderir.
 */
export function LoginScreen({ submitting, expired, problem, onSubmit, onRetryResume }: Props) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<LoginFormErrors>({});

  const submit = (event?: FormEvent) => {
    event?.preventDefault();
    const found = validateLoginForm(email, password);
    setErrors(found);
    if (!found.email && !found.password) {
      onSubmit(email.trim(), password);
    }
  };

  const message =
    problem?.kind === "server" ? problem.message : problem?.kind === "invalid" ? t("auth.invalidCredentials") : problem?.kind === "unreachable" ? t("auth.unreachable") : null;

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-labelledby="login-title">
      <h2 id="login-title">{t("auth.title")}</h2>
      {expired && (
        <p className="notice" role="status">
          {t("auth.expired")}
        </p>
      )}
      <label htmlFor="login-email">{t("auth.email")}</label>
      <input
        id="login-email"
        type="email"
        autoComplete="username"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        aria-invalid={errors.email ? true : undefined}
        aria-describedby={errors.email ? "login-email-error" : undefined}
      />
      {errors.email && (
        <p id="login-email-error" className="field-error" role="alert">
          {t(`auth.${errors.email}`)}
        </p>
      )}
      <label htmlFor="login-password">{t("auth.password")}</label>
      <input
        id="login-password"
        type="password"
        autoComplete="current-password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        aria-invalid={errors.password ? true : undefined}
        aria-describedby={errors.password ? "login-password-error" : undefined}
      />
      {errors.password && (
        <p id="login-password-error" className="field-error" role="alert">
          {t(`auth.${errors.password}`)}
        </p>
      )}
      {message && (
        <p className="form-error" role="alert">
          {message}
        </p>
      )}
      <div className="actions">
        <button type="submit" disabled={submitting} aria-busy={submitting}>
          {submitting ? t("auth.submitting") : t("auth.submit")}
        </button>
        {problem?.kind === "unreachable" && !submitting && (
          <button type="button" className="secondary" onClick={() => (problem.resume ? onRetryResume() : submit())}>
            {t("auth.retry")}
          </button>
        )}
      </div>
    </form>
  );
}
