import { useEffect, useState, type FormEvent } from "react";
import { dateRangeText, validateTermForm, type AuthSession, type TermAdminView, type TermFormValues } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "./adminApi";
import { academicFieldText, failureText } from "./adminText";
import { TermCalendar } from "./TermCalendar";

type Client = AuthSession["client"];
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; terms: TermAdminView[] };
const EMPTY: TermFormValues = { name: "", startDate: "", endDate: "" };

/**
 * Dönemler ve takvim sekmesi (spec 0008): dönem listesi (ad, tarihler, "Güncel" / "Yaklaşan" etiketi; yükleniyor / boş /
 * dolu / hata + tekrar dene), "Dönem ekle" ve "Düzenle" formu (ad, başlangıç, bitiş — tarayıcının tarih alanı), "Takvim"
 * o dönemin kayıtlarını açar (TermCalendar). Dönem silinmez.
 */
export function TermsTab({ client }: { client: Client }) {
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState<{ id: string | null; values: TermFormValues } | null>(null);
  const [calendarOf, setCalendarOf] = useState<TermAdminView | null>(null);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/admin/terms")).then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", terms: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  const refresh = () => {
    setList({ kind: "loading" });
    setReload((n) => n + 1);
  };

  if (list.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("admin.loading")}</p>;
  }

  if (list.kind === "error") {
    return (
      <div className="card">
        <p className="form-error" role="alert">{list.message}</p>
        <div className="actions">
          <button type="button" onClick={refresh}>{t("admin.retry")}</button>
        </div>
      </div>
    );
  }

  return (
    <div className="admin-section">
      <div className="actions">
        <button type="button" onClick={() => setEditing({ id: null, values: EMPTY })}>{t("terms.add")}</button>
      </div>
      {editing && (
        <TermForm
          key={editing.id ?? "new"}
          client={client}
          id={editing.id}
          initial={editing.values}
          onCancel={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            refresh();
          }}
        />
      )}
      {list.terms.length === 0 ? (
        <p>{t("terms.empty")}</p>
      ) : (
        <ul className="admin-list">
          {list.terms.map((term) => (
            <li key={term.id} className="admin-row">
              <span>
                <strong>{term.name}</strong>
                <span className="notice"> · {dateRangeText(term.startDate, term.endDate)}</span>
                {term.isCurrent && <span className="badge"> {t("terms.current")}</span>}
                {term.isUpcoming && <span className="badge"> {t("terms.upcoming")}</span>}
              </span>
              <span className="actions">
                <button type="button" className="secondary" onClick={() => setEditing({ id: term.id, values: { name: term.name, startDate: term.startDate, endDate: term.endDate } })}>
                  {t("academic.edit")}
                </button>
                <button type="button" className="secondary" aria-expanded={calendarOf?.id === term.id} onClick={() => setCalendarOf(calendarOf?.id === term.id ? null : term)}>
                  {t("terms.calendar")}
                </button>
              </span>
            </li>
          ))}
        </ul>
      )}
      {calendarOf && <TermCalendar key={calendarOf.id} client={client} term={calendarOf} onClose={() => setCalendarOf(null)} />}
    </div>
  );
}

interface FormProps {
  client: Client;
  id: string | null;
  initial: TermFormValues;
  onCancel: () => void;
  onSaved: () => void;
}

function TermForm({ client, id, initial, onCancel, onSaved }: FormProps) {
  const [values, setValues] = useState(initial);
  const [errors, setErrors] = useState<ReturnType<typeof validateTermForm>>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateTermForm(values);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const body = { name: values.name.trim(), startDate: values.startDate, endDate: values.endDate };
    const outcome = id === null
      ? await call(client.POST("/api/admin/terms", { body }))
      : await call(client.PUT("/api/admin/terms/{id}", { params: { path: { id } }, body }));
    setSaving(false);
    if (outcome.ok) {
      onSaved();
    } else {
      setServerError(failureText(outcome));
    }
  };

  const field = (name: keyof TermFormValues, label: string, type: "text" | "date") => {
    const inputId = `term-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={inputId}>{label}</label>
        <input
          id={inputId}
          type={type}
          value={values[name]}
          onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
        {error && <p id={`${inputId}-error`} className="field-error" role="alert">{academicFieldText(error, "terms")}</p>}
      </>
    );
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={id === null ? t("terms.add") : t("academic.edit")}>
      {field("name", t("terms.name"), "text")}
      {field("startDate", t("terms.start"), "date")}
      {field("endDate", t("terms.end"), "date")}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("academic.saving") : t("academic.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("academic.cancel")}</button>
      </div>
    </form>
  );
}
