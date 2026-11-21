import { useEffect, useState } from "react";
import { dayText, moneyText, moneyTl, toCents, type AuthSession, type PaymentRow, type PlanView, type StudentAccount as Account } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { ConfirmDialog } from "../admin/ConfirmDialog";
import { fill, methodText, stateText, turkeyDate } from "./billingText";
import { CancelPaymentDialog } from "./CancelPaymentDialog";
import { PaymentForm } from "./PaymentForm";
import { PlanForm } from "./PlanForm";

type Client = AuthSession["client"];
type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; account: Account };
type Panel = { kind: "none" } | { kind: "plan" } | { kind: "payment" };

/**
 * Öğrencinin carisi (spec 0015 gereksinim 4–8, UX): özet (borç, ödenen, kalan, geciken), planlar (taksit tablosu, durum metinle —
 * "Gecikti"; iptal plan "İptal"), "Plan aç" (önizlemeli form), "Tahsilat ekle" (geçerli plan varsa; kaydedince "Makbuza git"),
 * tahsilat satırında "Makbuz" ve "İptal et" (gerekçe + onay), planda "Planı iptal et" (onay), tarih sıralı hareket dökümü. Her yazma
 * işleminden sonra cari sunucudan yeniden okunur (eski ekrandan hesap yapılmaz).
 */
export function StudentAccount({ client, studentId }: { client: Client; studentId: string }) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [panel, setPanel] = useState<Panel>({ kind: "none" });
  const [message, setMessage] = useState<{ text: string; receiptId?: string } | null>(null);
  const [cancelPlan, setCancelPlan] = useState<PlanView | null>(null);
  const [cancelPayment, setCancelPayment] = useState<PaymentRow | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/billing/students/{id}", { params: { path: { id: studentId } } })).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", account: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, studentId, reload]);

  const refresh = () => setReload((n) => n + 1);

  const doCancelPlan = async (plan: PlanView) => {
    const outcome = await call(client.POST("/api/billing/plans/{id}/cancel", { params: { path: { id: plan.id } } }));
    setCancelPlan(null);
    if (outcome.ok) {
      setActionError(null);
      refresh();
    } else {
      setActionError(failureText(outcome));
    }
  };

  const back = (
    <a href="#/tahsilat">{t("billing.account.back")}</a>
  );

  if (state.kind === "loading") {
    return <section className="card"><p className="notice" aria-busy="true">{t("billing.loading")}</p></section>;
  }

  if (state.kind === "error") {
    return (
      <section className="card">
        <p className="form-error" role="alert">{state.message}</p>
        <div className="actions">
          <button type="button" onClick={() => { setState({ kind: "loading" }); refresh(); }}>{t("billing.retry")}</button>
          {back}
        </div>
      </section>
    );
  }

  const account = state.account;
  const activePlans = account.plans.filter((p) => p.status === "Active");
  return (
    <section className="card admin billing" aria-labelledby="account-title">
      <div className="admin-head">
        <h2 id="account-title">{fill("billing.account.title", { name: account.fullName })}</h2>
        {back}
      </div>
      <p className="notice">{account.classroomName ? `${account.branchName} / ${account.classroomName}` : account.branchName}</p>
      {!account.isActive && <p className="notice">{t("billing.account.left")}</p>}
      <dl className="facts" aria-label={t("billing.account.summary")}>
        <dt>{t("billing.col.net")}</dt>
        <dd>{moneyTl(account.totals.net)}</dd>
        <dt>{t("billing.col.paid")}</dt>
        <dd>{moneyTl(account.totals.paid)}</dd>
        <dt>{t("billing.col.remaining")}</dt>
        <dd>{moneyTl(account.totals.remaining)}</dd>
        <dt>{t("billing.col.overdue")}</dt>
        <dd>{moneyTl(account.totals.overdue)}</dd>
      </dl>
      <div className="actions">
        <button type="button" onClick={() => { setMessage(null); setPanel({ kind: "plan" }); }}>{t("billing.plan.open")}</button>
        {activePlans.length > 0 && (
          <button type="button" onClick={() => { setMessage(null); setPanel({ kind: "payment" }); }}>{t("billing.payments.add")}</button>
        )}
      </div>
      {message && (
        <p className="success" role="status">
          {message.text}
          {message.receiptId && <> <a href={`#/makbuz/${message.receiptId}`}>{t("billing.payment.toReceipt")}</a></>}
        </p>
      )}
      {actionError && <p className="form-error" role="alert">{actionError}</p>}
      {panel.kind === "plan" && (
        <PlanForm
          client={client}
          studentId={account.studentId}
          onCancel={() => setPanel({ kind: "none" })}
          onSaved={() => {
            setPanel({ kind: "none" });
            setMessage({ text: t("billing.plan.saved") });
            refresh();
          }}
        />
      )}
      {panel.kind === "payment" && (
        <PaymentForm
          client={client}
          plans={activePlans}
          onCancel={() => setPanel({ kind: "none" })}
          onSaved={(recorded) => {
            setPanel({ kind: "none" });
            setMessage({ text: fill("billing.payment.saved", { no: recorded.receiptNo }), receiptId: recorded.id });
            refresh();
          }}
        />
      )}

      <h3>{t("billing.account.plans")}</h3>
      {account.plans.length === 0 ? <p>{t("billing.account.noPlan")}</p> : account.plans.map((plan) => (
        <PlanBlock key={plan.id} plan={plan} onCancel={() => setCancelPlan(plan)} />
      ))}

      <h3>{t("billing.payments.title")}</h3>
      {account.payments.length === 0 ? <p>{t("billing.payments.empty")}</p> : (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th scope="col">{t("billing.col.receipt")}</th>
                <th scope="col">{t("billing.col.date")}</th>
                <th scope="col">{t("billing.col.plan")}</th>
                <th scope="col" className="num">{t("billing.col.amount")}</th>
                <th scope="col">{t("billing.col.method")}</th>
                <th scope="col">{t("billing.col.recordedBy")}</th>
                <th scope="col">{t("billing.col.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {account.payments.map((payment) => (
                <tr key={payment.id}>
                  <th scope="row">{payment.receiptNo}</th>
                  <td>{dayText(payment.date)}</td>
                  <td>{payment.planName}</td>
                  <td className="num">{moneyText(payment.amount)}</td>
                  <td>{methodText(payment.method)}</td>
                  <td>{payment.recordedByName}</td>
                  <td>
                    <span className="actions">
                      <a href={`#/makbuz/${payment.id}`} aria-label={fill("billing.payment.receiptOf", { no: payment.receiptNo })}>{t("billing.payment.receipt")}</a>
                      {payment.cancelled ? (
                        <span className="badge">{t("billing.payment.cancelled")}{payment.cancelledAt ? ` · ${turkeyDate(payment.cancelledAt)}` : ""}{payment.cancelReason ? ` · ${payment.cancelReason}` : ""}</span>
                      ) : (
                        <button type="button" className="secondary" aria-label={fill("billing.payment.cancelOf", { no: payment.receiptNo })} onClick={() => setCancelPayment(payment)}>
                          {t("billing.payment.cancel")}
                        </button>
                      )}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <h3>{t("billing.movements.title")}</h3>
      {account.movements.length === 0 ? <p>{t("billing.movements.empty")}</p> : (
        <div className="table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th scope="col">{t("billing.col.date")}</th>
                <th scope="col">{t("billing.col.description")}</th>
                <th scope="col" className="num">{t("billing.col.debit")}</th>
                <th scope="col" className="num">{t("billing.col.credit")}</th>
                <th scope="col" className="num">{t("billing.col.balance")}</th>
              </tr>
            </thead>
            <tbody>
              {account.movements.map((m, index) => (
                <tr key={`${m.date}-${m.kind}-${m.receiptNo ?? index}`}>
                  <th scope="row">{dayText(m.date)}</th>
                  <td>{m.kind === "Plan" ? fill("billing.movements.Plan", { name: m.planName }) : fill("billing.movements.Payment", { name: m.planName, no: m.receiptNo ?? "" })}</td>
                  <td className="num">{toCents(m.debit) > 0 ? moneyText(m.debit) : ""}</td>
                  <td className="num">{toCents(m.credit) > 0 ? moneyText(m.credit) : ""}</td>
                  <td className="num">{moneyText(m.balance)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {cancelPlan && <ConfirmDialog message={t("billing.plan.confirmCancel")} onCancel={() => setCancelPlan(null)} onConfirm={() => doCancelPlan(cancelPlan)} />}
      {cancelPayment && (
        <CancelPaymentDialog
          client={client}
          payment={cancelPayment}
          onCancel={() => setCancelPayment(null)}
          onDone={() => {
            setCancelPayment(null);
            refresh();
          }}
        />
      )}
    </section>
  );
}

function PlanBlock({ plan, onCancel }: { plan: PlanView; onCancel: () => void }) {
  const cancelled = plan.status === "Cancelled";
  return (
    <div className="plan-block">
      <h4>
        {plan.feeName}
        {cancelled && <span className="badge"> {t("billing.plan.cancelled")}</span>}
      </h4>
      <p className="notice">
        {fill("billing.plan.fee", { amount: moneyTl(plan.feeAmount) })}
        {plan.discountName && <> · {fill("billing.plan.discount", { name: plan.discountName, percent: plan.discountPercent ?? 0, amount: moneyTl(plan.discountAmount) })}</>}
        {" · "}{t("billing.col.net")}: {moneyTl(plan.totals.net)} · {t("billing.col.remaining")}: {moneyTl(plan.totals.remaining)} · {t("billing.col.overdue")}: {moneyTl(plan.totals.overdue)}
      </p>
      <div className="table-scroll">
        <table className="data-table">
          <thead>
            <tr>
              <th scope="col">{t("billing.col.sequence")}</th>
              <th scope="col">{t("billing.col.due")}</th>
              <th scope="col" className="num">{t("billing.col.amount")}</th>
              <th scope="col" className="num">{t("billing.col.paid")}</th>
              <th scope="col" className="num">{t("billing.col.remaining")}</th>
              <th scope="col">{t("billing.col.state")}</th>
            </tr>
          </thead>
          <tbody>
            {plan.installments.map((i) => (
              <tr key={i.sequence}>
                <th scope="row">{i.sequence}</th>
                <td>{dayText(i.dueDate)}</td>
                <td className="num">{moneyText(i.amount)}</td>
                <td className="num">{moneyText(i.paid)}</td>
                <td className="num">{moneyText(i.remaining)}</td>
                <td className={i.state === "Overdue" ? "state-overdue" : undefined}>{stateText(i.state)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {!cancelled && (
        <div className="actions">
          <button type="button" className="secondary" onClick={onCancel}>{t("billing.plan.cancel")}</button>
        </div>
      )}
    </div>
  );
}
