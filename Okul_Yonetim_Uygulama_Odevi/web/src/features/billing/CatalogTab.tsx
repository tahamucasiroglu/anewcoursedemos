import { useEffect, useState, type FormEvent } from "react";
import {
  moneyTl,
  parseAmount,
  parsePercent,
  validateDiscountForm,
  validateTariffForm,
  type AuthSession,
  type BillingFieldError,
  type DiscountRow,
  type FeeScheduleRow,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { call, type ApiOutcome } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { ConfirmDialog } from "../admin/ConfirmDialog";
import { billingFieldText } from "./billingText";

type Client = AuthSession["client"];
export type CatalogKind = "tariffs" | "discounts";
type Row = { id: string; name: string; value: string; isActive: boolean };
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; rows: Row[] };
interface Values { name: string; value: string }

const TEXT = {
  tariffs: { empty: "billing.tariffs.empty", add: "billing.tariffs.add", name: "billing.tariffs.name", value: "billing.tariffs.amount" },
  discounts: { empty: "billing.discounts.empty", add: "billing.discounts.add", name: "billing.discounts.name", value: "billing.discounts.percent" },
} as const;

/**
 * Tarife ve indirim sekmelerinin ortak gövdesi (spec 0015 gereksinim 1–2; yalnızca kurucu): liste (yükleniyor / boş / dolu / hata +
 * tekrar dene), ekle/düzenle formu (düzenlenen kayda bağlı anahtar — T15; alan hataları istek göndermeden; gönderilirken kilitli;
 * sunucu hatası duyurulur), pasifleştir (onaylı) / etkinleştir. Tutar "1.234,50" yazılır, sunucuya metin olarak ("1234.50") gider.
 */
export function CatalogTab({ client, kind }: { client: Client; kind: CatalogKind }) {
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState<{ id: string | null; values: Values } | null>(null);
  const [confirm, setConfirm] = useState<Row | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const text = TEXT[kind];

  useEffect(() => {
    let active = true;
    const request: Promise<ApiOutcome<Row[]>> = kind === "tariffs"
      ? call(client.GET("/api/billing/tariffs")).then((o) => (o.ok ? { ok: true, data: o.data.map(tariffRow) } : o))
      : call(client.GET("/api/billing/discounts")).then((o) => (o.ok ? { ok: true, data: o.data.map(discountRow) } : o));
    request.then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", rows: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, kind, reload]);

  const refresh = () => {
    setList({ kind: "loading" });
    setReload((n) => n + 1);
  };

  const setActive = async (row: Row, activate: boolean) => {
    const path = { params: { path: { id: row.id } } };
    const outcome = kind === "tariffs"
      ? await call(client.POST(activate ? "/api/billing/tariffs/{id}/activate" : "/api/billing/tariffs/{id}/deactivate", path))
      : await call(client.POST(activate ? "/api/billing/discounts/{id}/activate" : "/api/billing/discounts/{id}/deactivate", path));
    setConfirm(null);
    if (outcome.ok) {
      setActionError(null);
      refresh();
    } else {
      setActionError(failureText(outcome));
    }
  };

  if (list.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("billing.loading")}</p>;
  }

  if (list.kind === "error") {
    return (
      <div>
        <p className="form-error" role="alert">{list.message}</p>
        <div className="actions">
          <button type="button" onClick={refresh}>{t("billing.retry")}</button>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-section">
      <div className="actions">
        <button type="button" onClick={() => setEditing({ id: null, values: { name: "", value: "" } })}>{t(text.add)}</button>
      </div>
      {editing && (
        <CatalogForm
          key={editing.id ?? "new"}
          client={client}
          kind={kind}
          id={editing.id}
          initial={editing.values}
          onCancel={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            refresh();
          }}
        />
      )}
      {actionError && <p className="form-error" role="alert">{actionError}</p>}
      {list.rows.length === 0 ? (
        <p>{t(text.empty)}</p>
      ) : (
        <ul className="admin-list">
          {list.rows.map((row) => (
            <li key={row.id} className="admin-row">
              <span>
                <strong>{row.name}</strong> · {row.value}
                {!row.isActive && <span className="badge"> {t("billing.inactive")}</span>}
              </span>
              <span className="actions">
                <button type="button" className="secondary" aria-label={`${t("billing.edit")}: ${row.name}`} onClick={() => setEditing({ id: row.id, values: { name: row.name, value: editValue(kind, row) } })}>
                  {t("billing.edit")}
                </button>
                {row.isActive ? (
                  <button type="button" className="secondary" aria-label={`${t("billing.deactivate")}: ${row.name}`} onClick={() => setConfirm(row)}>{t("billing.deactivate")}</button>
                ) : (
                  <button type="button" className="secondary" aria-label={`${t("billing.activate")}: ${row.name}`} onClick={() => setActive(row, true)}>{t("billing.activate")}</button>
                )}
              </span>
            </li>
          ))}
        </ul>
      )}
      {confirm && <ConfirmDialog message={t("billing.confirmDeactivate")} onCancel={() => setConfirm(null)} onConfirm={() => setActive(confirm, false)} />}
    </div>
  );
}

function tariffRow(row: FeeScheduleRow): Row {
  return { id: row.id, name: row.name, value: moneyTl(row.amount), isActive: row.isActive };
}

function discountRow(row: DiscountRow): Row {
  return { id: row.id, name: row.name, value: `%${row.percent}`, isActive: row.isActive };
}

function editValue(kind: CatalogKind, row: Row): string {
  return kind === "tariffs" ? row.value.replace(/ TL$/, "") : row.value.replace(/^%/, "");
}

interface FormProps {
  client: Client;
  kind: CatalogKind;
  id: string | null;
  initial: Values;
  onCancel: () => void;
  onSaved: () => void;
}

function CatalogForm({ client, kind, id, initial, onCancel, onSaved }: FormProps) {
  const [values, setValues] = useState(initial);
  const [errors, setErrors] = useState<{ name?: BillingFieldError; value?: BillingFieldError }>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const text = TEXT[kind];

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = kind === "tariffs"
      ? (({ name, amount }) => ({ name, value: amount }))(validateTariffForm({ name: values.name, amount: values.value }))
      : (({ name, percent }) => ({ name, value: percent }))(validateDiscountForm({ name: values.name, percent: values.value }));
    const clean = Object.fromEntries(Object.entries(found).filter(([, v]) => v !== undefined)) as typeof found;
    setErrors(clean);
    if (Object.keys(clean).length > 0) {
      return;
    }

    setSaving(true);
    let outcome: ApiOutcome<unknown>;
    if (kind === "tariffs") {
      const amount = parseAmount(values.value);
      const body = { name: values.name.trim(), amount: amount.ok ? amount.text : values.value };
      outcome = id === null
        ? await call(client.POST("/api/billing/tariffs", { body }))
        : await call(client.PUT("/api/billing/tariffs/{id}", { params: { path: { id } }, body }));
    } else {
      const percent = parsePercent(values.value);
      const body = { name: values.name.trim(), percent: percent.ok ? percent.value : values.value };
      outcome = id === null
        ? await call(client.POST("/api/billing/discounts", { body }))
        : await call(client.PUT("/api/billing/discounts/{id}", { params: { path: { id } }, body }));
    }

    setSaving(false);
    if (outcome.ok) {
      onSaved();
    } else {
      setServerError(failureText(outcome));
    }
  };

  const field = (name: keyof Values, label: string, scope: "name" | "amount" | "percent") => {
    const inputId = `${kind}-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={inputId}>{label}</label>
        <input
          id={inputId}
          value={values[name]}
          inputMode={name === "value" ? "decimal" : undefined}
          onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
        {error && <p id={`${inputId}-error`} className="field-error" role="alert">{billingFieldText(error, scope)}</p>}
      </>
    );
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={id === null ? t(text.add) : t("billing.edit")}>
      {field("name", t(text.name), "name")}
      {field("value", t(text.value), kind === "tariffs" ? "amount" : "percent")}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("billing.saving") : t("billing.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("billing.cancel")}</button>
      </div>
    </form>
  );
}
