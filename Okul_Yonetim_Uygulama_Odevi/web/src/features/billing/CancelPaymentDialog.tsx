import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from "react";
import { validateCancelReason, type AuthSession, type BillingFieldError, type PaymentRow } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { billingFieldText, fill } from "./billingText";

interface Props {
  client: AuthSession["client"];
  payment: PaymentRow;
  onCancel: () => void;
  onDone: () => void;
}

/**
 * Tahsilat iptali iletişimi (spec 0015 gereksinim 6, UX): gerekçe (zorunlu, ≤ 200) + "Vazgeç" / "Tahsilatı iptal et". Açılınca odak
 * gerekçe alanına gelir; Tab iletişim içinde döner; Esc kapatır; kapanınca odak açan düğmeye döner (0005 onay iletişimi kalıbı).
 * Gerekçe hatası istek göndermeden; gönderilirken düğme kilitli; sunucu hatası duyurulur.
 */
export function CancelPaymentDialog({ client, payment, onCancel, onDone }: Props) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const reasonRef = useRef<HTMLTextAreaElement>(null);
  const onCancelRef = useRef(onCancel);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<BillingFieldError | undefined>(undefined);
  const [sending, setSending] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  useEffect(() => {
    onCancelRef.current = onCancel;
  }, [onCancel]);

  useEffect(() => {
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    reasonRef.current?.focus();
    const onKey = (event: globalThis.KeyboardEvent) => {
      if (event.key === "Escape") {
        onCancelRef.current();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => {
      window.removeEventListener("keydown", onKey);
      if (opener?.isConnected) {
        opener.focus();
      }
    };
  }, []);

  const trapTab = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key !== "Tab" || !dialogRef.current) {
      return;
    }

    const items = Array.from(dialogRef.current.querySelectorAll<HTMLElement>("textarea, button:not([disabled])"));
    const first = items[0];
    const last = items[items.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateCancelReason(reason);
    setError(found);
    if (found || sending) {
      return;
    }

    setSending(true);
    const outcome = await call(client.POST("/api/billing/payments/{id}/cancel", { params: { path: { id: payment.id } }, body: { reason: reason.trim() } }));
    setSending(false);
    if (outcome.ok) {
      onDone();
    } else {
      setServerError(failureText(outcome));
    }
  };

  return (
    <div className="dialog-backdrop">
      <div className="card dialog" role="dialog" aria-modal="true" aria-labelledby="cancel-payment-title" ref={dialogRef} onKeyDown={trapTab}>
        <form onSubmit={submit} noValidate>
          <h3 id="cancel-payment-title">{t("billing.cancelDialog.title")} — {fill("billing.payment.receiptOf", { no: payment.receiptNo })}</h3>
          <p>{t("billing.cancelDialog.text")}</p>
          <label htmlFor="cancel-reason">{t("billing.cancelDialog.reason")}</label>
          <textarea
            id="cancel-reason"
            ref={reasonRef}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? "cancel-reason-error" : undefined}
          />
          {error && <p id="cancel-reason-error" className="field-error" role="alert">{billingFieldText(error, "reason")}</p>}
          {serverError && <p className="form-error" role="alert">{serverError}</p>}
          <div className="actions">
            <button type="button" className="secondary" onClick={onCancel}>{t("billing.cancel")}</button>
            <button type="submit" disabled={sending} aria-busy={sending}>{sending ? t("billing.cancelDialog.sending") : t("billing.cancelDialog.confirm")}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
