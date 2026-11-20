import { useEffect, useState, type FormEvent } from "react";
import { validateBranchForm, type AuthSession, type BranchAdminView, type BranchFormValues } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "./adminApi";
import { failureText, fieldText } from "./adminText";
import { ConfirmDialog } from "./ConfirmDialog";

type Client = AuthSession["client"];
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; branches: BranchAdminView[] };
const EMPTY: BranchFormValues = { name: "", address: "", phone: "" };

/**
 * Şubeler sekmesi (spec 0005): liste (yükleniyor / boş / dolu / hata + tekrar dene), ekle ve düzenle formu (alan
 * hataları istek göndermeden; gönderilirken kilitli; sunucu hatası), pasifleştir (onaylı) / etkinleştir. Son etkin şubede
 * "Pasifleştir" pasif ve nedeni yazılı.
 */
export function BranchesTab({ client }: { client: Client }) {
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState<{ id: string | null; values: BranchFormValues } | null>(null);
  const [confirm, setConfirm] = useState<BranchAdminView | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    call(client.GET("/api/admin/branches")).then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", branches: outcome.data } : { kind: "error", message: failureText(outcome) });
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

  const setActive = async (branch: BranchAdminView, activate: boolean) => {
    const path = activate ? "/api/admin/branches/{id}/activate" : "/api/admin/branches/{id}/deactivate";
    const outcome = await call(client.POST(path, { params: { path: { id: branch.id } } }));
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

  const activeCount = list.branches.filter((b) => b.isActive).length;
  return (
    <div className="admin-section">
      <div className="actions">
        <button type="button" onClick={() => setEditing({ id: null, values: EMPTY })}>{t("branches.add")}</button>
      </div>
      {editing && (
        <BranchForm
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
      {list.branches.length === 0 ? (
        <p>{t("branches.empty")}</p>
      ) : (
        <ul className="admin-list">
          {list.branches.map((branch) => {
            const last = branch.isActive && activeCount <= 1;
            return (
              <li key={branch.id} className="admin-row">
                <span>
                  <strong>{branch.name}</strong>
                  {!branch.isActive && <span className="badge"> {t("branches.inactive")}</span>}
                  {branch.address && <span className="notice"> · {branch.address}</span>}
                  {branch.phone && <span className="notice"> · {branch.phone}</span>}
                </span>
                <span className="actions">
                  <button type="button" className="secondary" onClick={() => setEditing({ id: branch.id, values: { name: branch.name, address: branch.address ?? "", phone: branch.phone ?? "" } })}>
                    {t("branches.edit")}
                  </button>
                  {branch.isActive ? (
                    <button type="button" className="secondary" disabled={last} aria-describedby={last ? `last-${branch.id}` : undefined} onClick={() => setConfirm(branch)}>
                      {t("branches.deactivate")}
                    </button>
                  ) : (
                    <button type="button" className="secondary" onClick={() => setActive(branch, true)}>
                      {t("branches.activate")}
                    </button>
                  )}
                  {last && <span id={`last-${branch.id}`} className="notice">{t("branches.lastActive")}</span>}
                </span>
              </li>
            );
          })}
        </ul>
      )}
      {confirm && <ConfirmDialog message={t("branches.confirmDeactivate")} onCancel={() => setConfirm(null)} onConfirm={() => setActive(confirm, false)} />}
    </div>
  );
}

interface FormProps {
  client: Client;
  id: string | null;
  initial: BranchFormValues;
  onCancel: () => void;
  onSaved: () => void;
}

function BranchForm({ client, id, initial, onCancel, onSaved }: FormProps) {
  const [values, setValues] = useState(initial);
  const [errors, setErrors] = useState<ReturnType<typeof validateBranchForm>>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateBranchForm(values);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const body = { name: values.name.trim(), address: values.address.trim() || null, phone: values.phone.trim() || null };
    const outcome = id === null
      ? await call(client.POST("/api/admin/branches", { body }))
      : await call(client.PUT("/api/admin/branches/{id}", { params: { path: { id } }, body }));
    setSaving(false);
    if (outcome.ok) {
      onSaved();
    } else {
      setServerError(failureText(outcome));
    }
  };

  const field = (name: keyof BranchFormValues, label: string) => {
    const inputId = `branch-${name}`;
    const error = errors[name];
    return (
      <>
        <label htmlFor={inputId}>{label}</label>
        <input
          id={inputId}
          value={values[name]}
          onChange={(e) => setValues({ ...values, [name]: e.target.value })}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? `${inputId}-error` : undefined}
        />
        {error && <p id={`${inputId}-error`} className="field-error" role="alert">{fieldText(error)}</p>}
      </>
    );
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={id === null ? t("branches.add") : t("branches.edit")}>
      {field("name", t("branches.name"))}
      {field("address", t("branches.address"))}
      {field("phone", t("branches.phone"))}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("branches.saving") : t("branches.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("branches.cancel")}</button>
      </div>
    </form>
  );
}
