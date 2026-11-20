import { useEffect, useState, type FormEvent } from "react";
import {
  CALENDAR_KINDS,
  calendarKindKey,
  dateRangeText,
  validateCalendarEntryForm,
  type AuthSession,
  type CalendarEntryFormValues,
  type CalendarEntryView,
  type TermAdminView,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { call } from "./adminApi";
import { academicFieldText, failureText } from "./adminText";
import { ConfirmDialog } from "./ConfirmDialog";

type Client = AuthSession["client"];
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; entries: CalendarEntryView[] };
const EMPTY: CalendarEntryFormValues = { title: "", kind: "", startDate: "", endDate: "" };

function kindText(kind: string): string {
  return t(`calendar.kind.${calendarKindKey(kind)}` as TextKey);
}

interface Props {
  client: Client;
  term: TermAdminView;
  onClose: () => void;
}

/**
 * Bir dönemin akademik takvimi (spec 0008, Dönemler ve takvim sekmesinin alt paneli): kayıt listesi (tarih, tür metni,
 * başlık; başlangıç gününe göre; yükleniyor / boş / dolu / hata + tekrar dene), "Kayıt ekle" ve "Düzenle" formu (başlık,
 * tür, başlangıç, bitiş — iki gün de dönemin içinde), "Sil" (onaylı).
 */
export function TermCalendar({ client, term, onClose }: Props) {
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState<{ id: string | null; values: CalendarEntryFormValues } | null>(null);
  const [confirm, setConfirm] = useState<CalendarEntryView | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/admin/terms/{id}/calendar-entries", { params: { path: { id: term.id } } })).then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", entries: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, term.id, reload]);

  const refresh = () => {
    setList({ kind: "loading" });
    setReload((n) => n + 1);
  };

  const remove = async (entry: CalendarEntryView) => {
    const outcome = await call(client.DELETE("/api/admin/calendar-entries/{id}", { params: { path: { id: entry.id } } }));
    setConfirm(null);
    if (outcome.ok) {
      setActionError(null);
      refresh();
    } else {
      setActionError(failureText(outcome));
    }
  };

  return (
    <section className="card admin-section" aria-labelledby="term-calendar-title">
      <div className="admin-head">
        <h3 id="term-calendar-title">
          {t("calendarAdmin.heading")} {term.name}
        </h3>
        <button type="button" className="secondary" onClick={onClose}>{t("calendarAdmin.close")}</button>
      </div>
      {list.kind === "loading" && <p className="notice" aria-busy="true">{t("admin.loading")}</p>}
      {list.kind === "error" && (
        <>
          <p className="form-error" role="alert">{list.message}</p>
          <div className="actions">
            <button type="button" onClick={refresh}>{t("admin.retry")}</button>
          </div>
        </>
      )}
      {list.kind === "ready" && (
        <>
          <div className="actions">
            <button type="button" onClick={() => setEditing({ id: null, values: EMPTY })}>{t("calendarAdmin.add")}</button>
          </div>
          {editing && (
            <EntryForm
              key={editing.id ?? "new"}
              client={client}
              term={term}
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
          {list.entries.length === 0 ? (
            <p>{t("calendarAdmin.empty")}</p>
          ) : (
            <ul className="admin-list">
              {list.entries.map((entry) => (
                <li key={entry.id} className="admin-row">
                  <span>
                    <span className="notice">{dateRangeText(entry.startDate, entry.endDate)} · </span>
                    <span className="badge">{kindText(entry.kind)}</span> <strong>{entry.title}</strong>
                  </span>
                  <span className="actions">
                    <button
                      type="button"
                      className="secondary"
                      onClick={() => setEditing({ id: entry.id, values: { title: entry.title, kind: entry.kind, startDate: entry.startDate, endDate: entry.endDate } })}
                    >
                      {t("academic.edit")}
                    </button>
                    <button type="button" className="secondary" onClick={() => setConfirm(entry)}>{t("calendarAdmin.delete")}</button>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
      {confirm && <ConfirmDialog message={t("calendarAdmin.confirmDelete")} onCancel={() => setConfirm(null)} onConfirm={() => remove(confirm)} />}
    </section>
  );
}

interface FormProps {
  client: Client;
  term: TermAdminView;
  id: string | null;
  initial: CalendarEntryFormValues;
  onCancel: () => void;
  onSaved: () => void;
}

function EntryForm({ client, term, id, initial, onCancel, onSaved }: FormProps) {
  const [values, setValues] = useState(initial);
  const [errors, setErrors] = useState<ReturnType<typeof validateCalendarEntryForm>>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateCalendarEntryForm(values, term);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const body = { title: values.title.trim(), kind: values.kind, startDate: values.startDate, endDate: values.endDate };
    const outcome = id === null
      ? await call(client.POST("/api/admin/terms/{id}/calendar-entries", { params: { path: { id: term.id } }, body }))
      : await call(client.PUT("/api/admin/calendar-entries/{id}", { params: { path: { id } }, body }));
    setSaving(false);
    if (outcome.ok) {
      onSaved();
    } else {
      setServerError(failureText(outcome));
    }
  };

  const describe = (name: keyof CalendarEntryFormValues) => {
    const error = errors[name];
    const inputId = `entry-${name}`;
    return {
      id: inputId,
      "aria-invalid": error ? true : undefined,
      "aria-describedby": error ? `${inputId}-error` : undefined,
      error: error && <p id={`${inputId}-error`} className="field-error" role="alert">{academicFieldText(error, "calendarAdmin")}</p>,
    };
  };

  const input = (name: "title" | "startDate" | "endDate", label: string, type: "text" | "date") => {
    const { error, ...props } = describe(name);
    return (
      <>
        <label htmlFor={props.id}>{label}</label>
        <input {...props} type={type} value={values[name]} onChange={(e) => setValues({ ...values, [name]: e.target.value })} />
        {error}
      </>
    );
  };

  const kind = describe("kind");
  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={id === null ? t("calendarAdmin.add") : t("academic.edit")}>
      {input("title", t("calendarAdmin.title"), "text")}
      <label htmlFor={kind.id}>{t("calendarAdmin.kind")}</label>
      <select id={kind.id} aria-invalid={kind["aria-invalid"]} aria-describedby={kind["aria-describedby"]} value={values.kind} onChange={(e) => setValues({ ...values, kind: e.target.value })}>
        <option value="">{t("academic.choose")}</option>
        {CALENDAR_KINDS.map((k) => (
          <option key={k} value={k}>{kindText(k)}</option>
        ))}
      </select>
      {kind.error}
      {input("startDate", t("calendarAdmin.start"), "date")}
      {input("endDate", t("calendarAdmin.end"), "date")}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("academic.saving") : t("academic.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("academic.cancel")}</button>
      </div>
    </form>
  );
}
