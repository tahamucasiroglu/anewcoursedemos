import { useState, type FormEvent } from "react";
import { validateCode, type TwoFactorProblem } from "@okul/api-client";
import { t } from "../../i18n/t";

interface CodeFormProps {
  submitting: boolean;
  problem: TwoFactorProblem | null;
  onSubmit: (code: string) => void;
  onRestart: () => void;
}

/**
 * 6 haneli kod formu (spec 0007): sayısal klavye, tek kullanımlık kod otomatik doldurma; boş/hatalı biçimde istek
 * gönderilmez; "Kod hatalı", kilit ve ulaşılamıyor duyurulur; bekleme süresi dolduysa yalnızca "Girişe dön".
 */
export function CodeForm({ submitting, problem, onSubmit, onRestart }: CodeFormProps) {
  const [code, setCode] = useState("");
  const [error, setError] = useState(false);
  const expired = problem?.kind === "server" && problem.expired;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const invalid = validateCode(code) !== null;
    setError(invalid);
    if (!invalid) {
      onSubmit(code);
    }
  };

  const message = problem?.kind === "unreachable" ? t("auth.unreachable") : problem?.kind === "server" ? problem.message ?? t("twoFactor.failed") : null;

  return (
    <form onSubmit={submit} noValidate>
      {!expired && (
        <>
          <label htmlFor="two-factor-code">{t("twoFactor.code")}</label>
          <input
            id="two-factor-code"
            inputMode="numeric"
            autoComplete="one-time-code"
            maxLength={7}
            value={code}
            onChange={(e) => setCode(e.target.value)}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? "two-factor-code-error" : undefined}
          />
          {error && (
            <p id="two-factor-code-error" className="field-error" role="alert">
              {t("twoFactor.codeRequired")}
            </p>
          )}
        </>
      )}
      {message && (
        <p className="form-error" role="alert">
          {message}
        </p>
      )}
      <div className="actions">
        {!expired && (
          <button type="submit" disabled={submitting} aria-busy={submitting}>
            {submitting ? t("twoFactor.verifying") : t("twoFactor.verify")}
          </button>
        )}
        <button type="button" className="secondary" onClick={onRestart}>
          {t("twoFactor.backToLogin")}
        </button>
      </div>
    </form>
  );
}

/** Kodla giriş (spec 0007): platform yöneticisi şifreden sonra doğrulayıcı uygulamadaki kodu girer. */
export function TwoFactorScreen(props: CodeFormProps) {
  return (
    <section className="card auth-form" aria-labelledby="two-factor-title">
      <h2 id="two-factor-title">{t("twoFactor.title")}</h2>
      <p>{t("twoFactor.hint")}</p>
      <CodeForm {...props} />
    </section>
  );
}
