import { useEffect, useState, type FormEvent } from "react";
import { nextSortOrder, validateGradeLevelForm, type AuthSession, type GradeLevelAdminView, type GradeLevelFormValues } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "./adminApi";
import { academicFieldText, failureText } from "./adminText";
import { ConfirmDialog } from "./ConfirmDialog";

type Client = AuthSession["client"];
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; levels: GradeLevelAdminView[] };

/**
 * Sınıf seviyeleri sekmesi (spec 0008): liste (sıra, ad, durum; yükleniyor / boş / dolu / hata + tekrar dene), "Seviye
 * ekle" ve "Düzenle" formu (ad, sıra — varsayılan en büyük sıra + 1; alan hataları istek göndermeden; gönderilirken
 * kilitli; sunucu hatası), "Pasifleştir" (onaylı) / "Etkinleştir".
 */
export function GradeLevelsTab({ client }: { client: Client }) {
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState<{ id: string | null; values: GradeLevelFormValues } | null>(null);
  const [confirm, setConfirm] = useState<GradeLevelAdminView | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/admin/grade-levels")).then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", levels: outcome.data } : { kind: "error", message: failureText(outcome) });
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

  const setActive = async (level: GradeLevelAdminView, activate: boolean) => {
    const path = activate ? "/api/admin/grade-levels/{id}/activate" : "/api/admin/grade-levels/{id}/deactivate";
    const outcome = await call(client.POST(path, { params: { path: { id: level.id } } }));
    setConfirm(null);
    if (outcome.ok) {
      setActionError(null);
      refresh();
    } else {
      setActionError(failureText(outcome));
    }
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
        <button type="button" onClick={() => setEditing({ id: null, values: { name: "", sortOrder: String(nextSortOrder(list.levels)) } })}>
          {t("levels.add")}
        </button>
      </div>
      {editing && (
        <GradeLevelForm
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
      {actionError && <p className="form-error" role="alert">{actionError}</p>}
      {list.levels.length === 0 ? (
        <p>{t("levels.empty")}</p>
      ) : (
        <ul className="admin-list">
          {list.levels.map((level) => (
            <li key={level.id} className="admin-row">
              <span>
                <span className="notice">{t("levels.order")} {String(level.sortOrder)} · </span>
                <strong>{level.name}</strong>
                {!level.isActive && <span className="badge"> {t("academic.inactive")}</span>}
              </span>
              <span className="actions">
                <button type="button" className="secondary" onClick={() => setEditing({ id: level.id, values: { name: level.name, sortOrder: String(level.sortOrder) } })}>
                  {t("academic.edit")}
                </button>
                {level.isActive ? (
                  <button type="button" className="secondary" onClick={() => setConfirm(level)}>{t("academic.deactivate")}</button>
                ) : (
                  <button type="button" className="secondary" onClick={() => setActive(level, true)}>{t("academic.activate")}</button>
                )}
              </span>
            </li>
          ))}
        </ul>
      )}
      {confirm && <ConfirmDialog message={t("levels.confirmDeactivate")} onCancel={() => setConfirm(null)} onConfirm={() => setActive(confirm, false)} />}
    </div>
  );
}

interface FormProps {
  client: Client;
  id: string | null;
  initial: GradeLevelFormValues;
  onCancel: () => void;
  onSaved: () => void;
}

function GradeLevelForm({ client, id, initial, onCancel, onSaved }: FormProps) {
  const [values, setValues] = useState(initial);
  const [errors, setErrors] = useState<ReturnType<typeof validateGradeLevelForm>>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateGradeLevelForm(values);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const body = { name: values.name.trim(), sortOrder: Number(values.sortOrder.trim()) };
    const outcome = id === null
      ? await call(client.POST("/api/admin/grade-levels", { body }))
      : await call(client.PUT("/api/admin/grade-levels/{id}", { params: { path: { id } }, body }));
    setSaving(false);
    if (outcome.ok) {
      onSaved();
    } else {
      setServerError(failureText(outcome));
    }
  };

  const field = (name: keyof GradeLevelFormValues, label: string, inputMode?: "numeric") => {
    const inputId = `level-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={inputId}>{label}</label>
        <input
          id={inputId}
          inputMode={inputMode}
          value={values[name]}
          onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
        {error && <p id={`${inputId}-error`} className="field-error" role="alert">{academicFieldText(error, "levels")}</p>}
      </>
    );
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={id === null ? t("levels.add") : t("academic.edit")}>
      {field("name", t("levels.name"))}
      {field("sortOrder", t("levels.sortOrder"), "numeric")}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("academic.saving") : t("academic.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("academic.cancel")}</button>
      </div>
    </form>
  );
}
