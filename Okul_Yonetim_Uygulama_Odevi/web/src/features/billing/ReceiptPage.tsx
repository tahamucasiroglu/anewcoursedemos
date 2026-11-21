import { useEffect, useState } from "react";
import { dayText, moneyTl, type AuthSession, type Me, type Receipt } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { fill, methodText, turkeyDate } from "./billingText";

interface Props {
  me: Me;
  client: AuthSession["client"];
  paymentId: string;
}

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; receipt: Receipt };

/**
 * Makbuz sayfası "#/makbuz/&lt;id&gt;" (spec 0015 gereksinim 7, C-5): okul ve şube adı, makbuz no, tarih, öğrenci, plan, tutar, yöntem,
 * dağıldığı taksitler, kaydeden ve "Bu belge fatura yerine geçmez."; iptal edildiyse "İPTAL EDİLDİ" (tarih, gerekçe). "Yazdır"
 * tarayıcının yazdır / PDF olarak kaydet işlevini açar; yazdırma görünümünde menü ve düğmeler basılmaz (<c>@media print</c>,
 * <c>no-print</c>). Kurucu ya da muhasebe olmayan "Bu sayfaya erişiminiz yok." görür.
 */
export function ReceiptPage({ me, client, paymentId }: Props) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const allowed = me.roles.includes("Founder") || me.roles.includes("Accounting");

  useEffect(() => {
    if (!allowed) {
      return undefined;
    }

    let active = true;
    call(client.GET("/api/billing/payments/{id}/receipt", { params: { path: { id: paymentId } } })).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", receipt: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, paymentId, allowed]);

  if (!allowed) {
    return (
      <section className="card">
        <p role="alert">{t("billing.noAccess")}</p>
        <div className="actions">
          <a href="#/">{t("billing.back")}</a>
        </div>
      </section>
    );
  }

  if (state.kind === "loading") {
    return <section className="card"><p className="notice" aria-busy="true">{t("billing.loading")}</p></section>;
  }

  if (state.kind === "error") {
    return (
      <section className="card">
        <p className="form-error" role="alert">{state.message}</p>
        <div className="actions no-print">
          <a href="#/tahsilat">{t("billing.receipt.back")}</a>
        </div>
      </section>
    );
  }

  const r = state.receipt;
  return (
    <section className="card receipt" aria-labelledby="receipt-title">
      <div className="admin-head">
        <h2 id="receipt-title">{t("billing.receipt.title")}</h2>
        <div className="actions no-print">
          <button type="button" onClick={() => window.print()}>{t("billing.receipt.print")}</button>
          <a href="#/tahsilat">{t("billing.receipt.back")}</a>
        </div>
      </div>
      {r.cancelled && (
        <div className="receipt-cancelled" role="note">
          <p><strong>{t("billing.receipt.cancelled")}</strong></p>
          <p>{fill("billing.receipt.cancelledAt", { date: r.cancelledAt ? turkeyDate(r.cancelledAt) : "", reason: r.cancelReason ?? "" })}</p>
        </div>
      )}
      <dl className="facts">
        <dt>{t("billing.receipt.school")}</dt>
        <dd>{r.schoolName}</dd>
        <dt>{t("billing.receipt.branch")}</dt>
        <dd>{r.branchName}</dd>
        <dt>{t("billing.receipt.no")}</dt>
        <dd>{r.receiptNo}</dd>
        <dt>{t("billing.receipt.date")}</dt>
        <dd>{dayText(r.date)}</dd>
        <dt>{t("billing.receipt.student")}</dt>
        <dd>{r.studentName}</dd>
        <dt>{t("billing.receipt.plan")}</dt>
        <dd>{r.planName}</dd>
        <dt>{t("billing.receipt.amount")}</dt>
        <dd>{moneyTl(r.amount)}</dd>
        <dt>{t("billing.receipt.method")}</dt>
        <dd>{methodText(r.method)}</dd>
        <dt>{t("billing.receipt.allocations")}</dt>
        <dd>
          <ul className="branch-list">
            {r.allocations.map((a) => (
              <li key={a.sequence}>{fill("billing.receipt.allocation", { seq: a.sequence, due: dayText(a.dueDate), amount: moneyTl(a.amount) })}</li>
            ))}
          </ul>
        </dd>
        <dt>{t("billing.receipt.recordedBy")}</dt>
        <dd>{r.recordedByName}</dd>
      </dl>
      <p className="receipt-note">{t("billing.receipt.notInvoice")}</p>
    </section>
  );
}
