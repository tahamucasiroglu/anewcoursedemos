import { useState, type FormEvent } from "react";
import {
  localToday,
  moneyTl,
  PAYMENT_METHODS,
  paymentBody,
  validatePaymentForm,
  type AuthSession,
  type PaymentFormErrors,
  type PaymentFormValues,
  type PaymentRecorded,
  type PlanView,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { billingFieldText, fill, methodText } from "./billingText";

interface Props {
  client: AuthSession["client"];
  plans: PlanView[];
  onCancel: () => void;
  onSaved: (recorded: PaymentRecorded) => void;
}

/**
 * Tahsilat ekle formu (spec 0015 gereksinim 5, UX): plan (birden çok geçerli plan varsa), tutar ("1.234,50"; seçili planın kalan
 * borcunu aşamaz — kuruşla), tarih (bugün ya da en fazla 365 gün önce), yöntem, isteğe bağlı not (≤ 200). Alan hataları istek
 * göndermeden; gönderilirken düğme kilitli (çift gönderim yok); sunucu hatası ("Tutar kalan borçtan fazla olamaz." — başka biri
 * araya tahsilat girdiyse) duyurulur. Tutar sunucuya metin olarak gider.
 */
export function PaymentForm({ client, plans, onCancel, onSaved }: Props) {
  const [planId, setPlanId] = useState(plans[0]?.id ?? "");
  const [values, setValues] = useState<PaymentFormValues>({ amount: "", date: localToday(), method: "", note: "" });
  const [errors, setErrors] = useState<PaymentFormErrors>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const plan = plans.find((p) => p.id === planId) ?? null;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (saving) {
      return;
    }

    const found = validatePaymentForm(values, localToday(), plan?.totals.remaining ?? null);
    setErrors(found);
    if (Object.keys(found).length > 0 || !plan) {
      return;
    }

    setSaving(true);
    const outcome = await call(client.POST("/api/billing/payments", { body: paymentBody(plan.id, values) }));
    setSaving(false);
    if (outcome.ok) {
      onSaved(outcome.data);
    } else {
      setServerError(failureText(outcome));
    }
  };

  const describe = (id: string, error: string | undefined) => ({
    "aria-invalid": error ? true : undefined,
    "aria-describedby": error ? `${id}-error` : undefined,
  });

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={t("billing.payment.formTitle")}>
      <h3>{t("billing.payment.formTitle")}</h3>
      {plans.length > 1 && (
        <>
          <label htmlFor="payment-plan">{t("billing.payment.plan")}</label>
          <select id="payment-plan" value={planId} onChange={(e) => setPlanId(e.target.value)}>
            {plans.map((p) => (
              <option key={p.id} value={p.id}>{p.feeName}</option>
            ))}
          </select>
        </>
      )}
      {plan && <p className="notice">{plan.feeName} · {fill("billing.payment.remaining", { amount: moneyTl(plan.totals.remaining) })}</p>}

      <label htmlFor="payment-amount">{t("billing.payment.amount")}</label>
      <input id="payment-amount" inputMode="decimal" value={values.amount} onChange={(e) => setValues({ ...values, amount: e.target.value })} {...describe("payment-amount", errors.amount)} />
      {errors.amount && <p id="payment-amount-error" className="field-error" role="alert">{billingFieldText(errors.amount, "amount")}</p>}

      <label htmlFor="payment-date">{t("billing.payment.date")}</label>
      <input id="payment-date" type="date" value={values.date} onChange={(e) => setValues({ ...values, date: e.target.value })} {...describe("payment-date", errors.date)} />
      {errors.date && <p id="payment-date-error" className="field-error" role="alert">{billingFieldText(errors.date, "date")}</p>}

      <label htmlFor="payment-method">{t("billing.payment.method")}</label>
      <select id="payment-method" value={values.method} onChange={(e) => setValues({ ...values, method: e.target.value as PaymentFormValues["method"] })} {...describe("payment-method", errors.method)}>
        <option value="">{t("billing.payment.chooseMethod")}</option>
        {PAYMENT_METHODS.map((m) => (
          <option key={m} value={m}>{methodText(m)}</option>
        ))}
      </select>
      {errors.method && <p id="payment-method-error" className="field-error" role="alert">{billingFieldText(errors.method, "method")}</p>}

      <label htmlFor="payment-note">{t("billing.payment.note")}</label>
      <textarea id="payment-note" value={values.note} onChange={(e) => setValues({ ...values, note: e.target.value })} {...describe("payment-note", errors.note)} />
      {errors.note && <p id="payment-note-error" className="field-error" role="alert">{billingFieldText(errors.note, "note")}</p>}

      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("billing.saving") : t("billing.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("billing.cancel")}</button>
      </div>
    </form>
  );
}
