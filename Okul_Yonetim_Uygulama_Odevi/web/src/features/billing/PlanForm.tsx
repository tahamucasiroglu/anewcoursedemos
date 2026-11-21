import { useEffect, useState, type FormEvent } from "react";
import {
  dayText,
  localToday,
  moneyText,
  moneyTl,
  planBody,
  validatePlanForm,
  type AuthSession,
  type DiscountRow,
  type FeeScheduleRow,
  type PlanFormErrors,
  type PlanFormValues,
  type PlanPreview,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { billingFieldText, fill } from "./billingText";

type Client = AuthSession["client"];
type Catalog = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; fees: FeeScheduleRow[]; discounts: DiscountRow[] };

interface Props {
  client: Client;
  studentId: string;
  onCancel: () => void;
  onSaved: () => void;
}

/**
 * Plan aç formu (spec 0015 gereksinim 3, UX): etkin tarife (zorunlu), isteğe bağlı etkin indirim, taksit sayısı (1–12) ve ilk vade
 * (bugün ile 365 gün sonrası). "Önizle" sunucunun hesabını gösterir (indirim, net, taksit tablosu — kaydetmez); "Planı aç" kaydeder.
 * Alan hataları istek göndermeden; gönderilirken düğme kilitli; sunucu hatası (ör. "Net tutar 0 TL; …") duyurulur. Değer
 * değişince eski önizleme kaldırılır.
 */
export function PlanForm({ client, studentId, onCancel, onSaved }: Props) {
  const [catalog, setCatalog] = useState<Catalog>({ kind: "loading" });
  const [values, setValues] = useState<PlanFormValues>({ feeScheduleId: "", discountId: "", installmentCount: "9", firstDueDate: localToday() });
  const [errors, setErrors] = useState<PlanFormErrors>({});
  const [preview, setPreview] = useState<PlanPreview | null>(null);
  const [busy, setBusy] = useState<"preview" | "save" | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    Promise.all([call(client.GET("/api/billing/tariffs")), call(client.GET("/api/billing/discounts"))]).then(([fees, discounts]) => {
      if (!active) {
        return;
      }

      if (fees.ok && discounts.ok) {
        setCatalog({ kind: "ready", fees: fees.data.filter((f) => f.isActive), discounts: discounts.data.filter((d) => d.isActive) });
      } else {
        setCatalog({ kind: "error", message: failureText(fees.ok ? (discounts as Exclude<typeof discounts, { ok: true }>) : fees) });
      }
    });
    return () => {
      active = false;
    };
  }, [client]);

  const change = (patch: Partial<PlanFormValues>) => {
    setValues({ ...values, ...patch });
    setPreview(null);
    setServerError(null);
  };

  const check = () => {
    const found = validatePlanForm(values, localToday());
    setErrors(found);
    return Object.keys(found).length === 0;
  };

  const runPreview = async () => {
    if (!check()) {
      return;
    }

    setBusy("preview");
    const outcome = await call(client.POST("/api/billing/plans/preview", { body: planBody(studentId, values) }));
    setBusy(null);
    if (outcome.ok) {
      setPreview(outcome.data);
      setServerError(null);
    } else {
      setPreview(null);
      setServerError(failureText(outcome));
    }
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!check()) {
      return;
    }

    setBusy("save");
    const outcome = await call(client.POST("/api/billing/plans", { body: planBody(studentId, values) }));
    setBusy(null);
    if (outcome.ok) {
      onSaved();
    } else {
      setServerError(failureText(outcome));
    }
  };

  if (catalog.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("billing.loading")}</p>;
  }

  if (catalog.kind === "error") {
    return <p className="form-error" role="alert">{catalog.message}</p>;
  }

  const describe = (id: string, error: string | undefined) => ({
    "aria-invalid": error ? true : undefined,
    "aria-describedby": error ? `${id}-error` : undefined,
  });

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={t("billing.plan.formTitle")}>
      <h3>{t("billing.plan.formTitle")}</h3>
      {catalog.fees.length === 0 && <p className="notice">{t("billing.plan.noCatalog")}</p>}
      <label htmlFor="plan-fee">{t("billing.plan.feeLabel")}</label>
      <select id="plan-fee" value={values.feeScheduleId} onChange={(e) => change({ feeScheduleId: e.target.value })} {...describe("plan-fee", errors.feeScheduleId)}>
        <option value="">{t("billing.plan.chooseFee")}</option>
        {catalog.fees.map((f) => (
          <option key={f.id} value={f.id}>{`${f.name} — ${moneyTl(f.amount)}`}</option>
        ))}
      </select>
      {errors.feeScheduleId && <p id="plan-fee-error" className="field-error" role="alert">{billingFieldText(errors.feeScheduleId, "fee")}</p>}

      <label htmlFor="plan-discount">{t("billing.plan.discountLabel")}</label>
      <select id="plan-discount" value={values.discountId} onChange={(e) => change({ discountId: e.target.value })}>
        <option value="">{t("billing.plan.noDiscount")}</option>
        {catalog.discounts.map((d) => (
          <option key={d.id} value={d.id}>{`${d.name} — %${d.percent}`}</option>
        ))}
      </select>

      <label htmlFor="plan-count">{t("billing.plan.count")}</label>
      <input id="plan-count" inputMode="numeric" value={values.installmentCount} onChange={(e) => change({ installmentCount: e.target.value.trim() })} {...describe("plan-count", errors.installmentCount)} />
      {errors.installmentCount && <p id="plan-count-error" className="field-error" role="alert">{billingFieldText(errors.installmentCount, "count")}</p>}

      <label htmlFor="plan-due">{t("billing.plan.firstDue")}</label>
      <input id="plan-due" type="date" value={values.firstDueDate} onChange={(e) => change({ firstDueDate: e.target.value })} {...describe("plan-due", errors.firstDueDate)} />
      {errors.firstDueDate && <p id="plan-due-error" className="field-error" role="alert">{billingFieldText(errors.firstDueDate, "due")}</p>}

      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      {preview && (
        <div className="plan-preview" aria-live="polite">
          <h4>{t("billing.plan.previewTitle")}</h4>
          <p>{fill("billing.plan.previewSummary", { fee: moneyTl(preview.feeAmount), discount: moneyTl(preview.discountAmount), net: moneyTl(preview.net) })}</p>
          <div className="table-scroll">
            <table className="data-table">
              <thead>
                <tr>
                  <th scope="col">{t("billing.col.sequence")}</th>
                  <th scope="col">{t("billing.col.due")}</th>
                  <th scope="col" className="num">{t("billing.col.amount")}</th>
                </tr>
              </thead>
              <tbody>
                {preview.installments.map((line) => (
                  <tr key={line.sequence}>
                    <th scope="row">{line.sequence}</th>
                    <td>{dayText(line.dueDate)}</td>
                    <td className="num">{moneyText(line.amount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
      <div className="actions">
        <button type="button" className="secondary" disabled={busy !== null} aria-busy={busy === "preview"} onClick={runPreview}>
          {busy === "preview" ? t("billing.plan.previewing") : t("billing.plan.preview")}
        </button>
        <button type="submit" disabled={busy !== null} aria-busy={busy === "save"}>{busy === "save" ? t("billing.saving") : t("billing.plan.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("billing.cancel")}</button>
      </div>
    </form>
  );
}
