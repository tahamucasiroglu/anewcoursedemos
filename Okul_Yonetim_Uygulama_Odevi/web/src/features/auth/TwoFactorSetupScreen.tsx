import { useEffect, useState } from "react";
import QRCode from "qrcode";
import type { AuthSession, TwoFactorProblem, TwoFactorSetupInfo } from "@okul/api-client";
import { t } from "../../i18n/t";
import { CodeForm } from "./TwoFactorScreen";

interface Props {
  session: AuthSession;
  challenge: string;
  submitting: boolean;
  problem: TwoFactorProblem | null;
  onSubmit: (code: string) => void;
  onRestart: () => void;
}

type SetupState =
  | { kind: "loading" }
  | { kind: "failed"; message: string; expired: boolean }
  | { kind: "ready"; setup: TwoFactorSetupInfo; qr: string | null };

/**
 * İki adımlı doğrulama kurulumu (spec 0007): kare kod (doğrulayıcı uygulama okur) ve elle yazılabilir anahtar
 * (kamera yoksa / ekran okuyucu); uygulamadaki 6 haneli kodla tamamlanır. Anahtar yalnızca bu ekranda, bu girişte
 * gösterilir; kare kod tarayıcıda üretilir, anahtar sayfadan dışarı gitmez.
 */
export function TwoFactorSetupScreen({ session, challenge, submitting, problem, onSubmit, onRestart }: Props) {
  const [setup, setSetup] = useState<SetupState>({ kind: "loading" });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    session.beginTwoFactorSetup(challenge).then(async (outcome) => {
      if (!active) {
        return;
      }

      if (outcome.kind !== "ok") {
        setSetup({
          kind: "failed",
          message: outcome.kind === "unreachable" ? t("auth.unreachable") : outcome.message ?? t("twoFactor.failed"),
          expired: outcome.kind === "rejected" && outcome.status === 401,
        });
        return;
      }

      // Kare kod üretilemezse elle yazılabilir anahtar yine gösterilir.
      const qr = await QRCode.toString(outcome.setup.otpAuthUri, { type: "svg", margin: 1 })
        .then((svg) => "data:image/svg+xml;utf8," + encodeURIComponent(svg))
        .catch(() => null);

      if (active) {
        setSetup({ kind: "ready", setup: outcome.setup, qr });
      }
    });
    return () => {
      active = false;
    };
  }, [session, challenge, attempt]);

  return (
    <section className="card auth-form" aria-labelledby="two-factor-setup-title">
      <h2 id="two-factor-setup-title">{t("twoFactor.setupTitle")}</h2>
      {setup.kind === "loading" && <p className="notice" aria-busy="true">{t("twoFactor.setupLoading")}</p>}
      {setup.kind === "failed" && (
        <>
          <p className="form-error" role="alert">{setup.message}</p>
          <div className="actions">
            {!setup.expired && (
              <button type="button" onClick={() => { setSetup({ kind: "loading" }); setAttempt((n) => n + 1); }}>
                {t("auth.retry")}
              </button>
            )}
            <button type="button" className="secondary" onClick={onRestart}>{t("twoFactor.backToLogin")}</button>
          </div>
        </>
      )}
      {setup.kind === "ready" && (
        <>
          <p>{t("twoFactor.setupHint")}</p>
          {setup.qr && <img className="qr" src={setup.qr} alt={t("twoFactor.qrAlt")} width={200} height={200} />}
          <p>{t("twoFactor.manualKey")}</p>
          <p className="secret-key">
            <code>{setup.setup.secret.replace(/(.{4})/g, "$1 ").trim()}</code>
          </p>
          <CodeForm submitting={submitting} problem={problem} onSubmit={onSubmit} onRestart={onRestart} />
        </>
      )}
    </section>
  );
}
