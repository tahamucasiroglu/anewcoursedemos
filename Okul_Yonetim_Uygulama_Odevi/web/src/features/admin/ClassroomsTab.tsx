import { useEffect, useState, type FormEvent } from "react";
import {
  validateClassroomForm,
  type AuthSession,
  type BranchAdminView,
  type ClassroomAdminList,
  type ClassroomAdminView,
  type ClassroomFormValues,
  type GradeLevelAdminView,
  type TermAdminView,
} from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "./adminApi";
import { academicFieldText, failureText } from "./adminText";
import { ConfirmDialog } from "./ConfirmDialog";

type Client = AuthSession["client"];
type Meta = { terms: TermAdminView[]; levels: GradeLevelAdminView[]; branches: BranchAdminView[] };
type MetaState = { kind: "loading" } | { kind: "error"; message: string } | ({ kind: "ready" } & Meta);
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; list: ClassroomAdminList };
interface Filters { termId: string; branchId: string; status: string }

/**
 * Sınıflar sekmesi (spec 0008, şube-sınıf eşleşmesi): üstte dönem seçimi (varsayılan güncel dönem) ve şube/durum
 * süzgeci; liste (şube, seviye, ad, durum; pasif şubenin sınıfı "şube pasif"; yükleniyor / boş / dolu / hata + tekrar
 * dene); "Sınıf ekle" (şube, seviye, ad; dönem seçili dönem) ve "Düzenle" (ad, seviye); "Pasifleştir" (onaylı) /
 * "Etkinleştir". Dönem yoksa "Önce bir dönem ekleyin"; etkin seviye yoksa "Sınıf ekle" pasif ve nedeni yazılı. Pasif şube
 * ve seviye formlarda seçenek olarak çıkmaz.
 */
export function ClassroomsTab({ client }: { client: Client }) {
  const [meta, setMeta] = useState<MetaState>({ kind: "loading" });
  const [metaReload, setMetaReload] = useState(0);
  const [filters, setFilters] = useState<Filters>({ termId: "", branchId: "", status: "" });
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [editing, setEditing] = useState<{ classroom: ClassroomAdminView | null; values: ClassroomFormValues } | null>(null);
  const [confirm, setConfirm] = useState<ClassroomAdminView | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    Promise.all([call(client.GET("/api/admin/terms")), call(client.GET("/api/admin/grade-levels")), call(client.GET("/api/admin/branches"))]).then(
      ([terms, levels, branches]) => {
        if (!active) {
          return;
        }

        if (terms.ok && levels.ok && branches.ok) {
          setMeta({ kind: "ready", terms: terms.data, levels: levels.data, branches: branches.data });
        } else {
          const failed = !terms.ok ? terms : !levels.ok ? levels : branches;
          setMeta({ kind: "error", message: failed.ok ? t("admin.loadError") : failureText(failed) });
        }
      },
    );
    return () => {
      active = false;
    };
  }, [client, metaReload]);

  const hasTerms = meta.kind === "ready" && meta.terms.length > 0;
  useEffect(() => {
    if (!hasTerms) {
      return;
    }

    let active = true;
    const query = {
      termId: filters.termId || undefined,
      branchId: filters.branchId || undefined,
      active: filters.status === "" ? undefined : filters.status === "active",
    };
    call(client.GET("/api/admin/classrooms", { params: { query } })).then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", list: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, filters, reload, hasTerms]);

  const refresh = () => {
    setList({ kind: "loading" });
    setReload((n) => n + 1);
  };

  const change = (next: Partial<Filters>) => {
    setList({ kind: "loading" });
    setFilters((current) => ({ ...current, ...next }));
  };

  const setActive = async (classroom: ClassroomAdminView, activate: boolean) => {
    const path = activate ? "/api/admin/classrooms/{id}/activate" : "/api/admin/classrooms/{id}/deactivate";
    const outcome = await call(client.POST(path, { params: { path: { id: classroom.id } } }));
    setConfirm(null);
    if (outcome.ok) {
      setActionError(null);
      refresh();
    } else {
      setActionError(failureText(outcome));
    }
  };

  if (meta.kind === "loading") {
    return <p className="notice" aria-busy="true">{t("admin.loading")}</p>;
  }

  if (meta.kind === "error") {
    return (
      <div className="card">
        <p className="form-error" role="alert">{meta.message}</p>
        <div className="actions">
          <button type="button" onClick={() => { setMeta({ kind: "loading" }); setMetaReload((n) => n + 1); }}>{t("admin.retry")}</button>
        </div>
      </div>
    );
  }

  if (meta.terms.length === 0) {
    return <p>{t("classrooms.noTerm")}</p>;
  }

  const activeLevels = meta.levels.filter((l) => l.isActive);
  const selectedTerm = filters.termId || (list.kind === "ready" ? list.list.termId ?? "" : "");
  return (
    <div className="admin-section">
      <div className="filters">
        <label htmlFor="classroom-term">{t("classrooms.term")}</label>
        <select id="classroom-term" value={selectedTerm} onChange={(e) => change({ termId: e.target.value })}>
          {selectedTerm === "" && <option value="">{t("academic.choose")}</option>}
          {meta.terms.map((term) => (
            <option key={term.id} value={term.id}>{term.name}</option>
          ))}
        </select>
        <label htmlFor="classroom-branch">{t("classrooms.branchFilter")}</label>
        <select id="classroom-branch" value={filters.branchId} onChange={(e) => change({ branchId: e.target.value })}>
          <option value="">{t("classrooms.all")}</option>
          {meta.branches.map((branch) => (
            <option key={branch.id} value={branch.id}>{branch.name}</option>
          ))}
        </select>
        <label htmlFor="classroom-status">{t("classrooms.statusFilter")}</label>
        <select id="classroom-status" value={filters.status} onChange={(e) => change({ status: e.target.value })}>
          <option value="">{t("classrooms.all")}</option>
          <option value="active">{t("classrooms.activeOnly")}</option>
          <option value="inactive">{t("classrooms.inactiveOnly")}</option>
        </select>
      </div>
      <div className="actions">
        <button
          type="button"
          disabled={activeLevels.length === 0 || list.kind !== "ready"}
          aria-describedby={activeLevels.length === 0 ? "classroom-no-level" : undefined}
          onClick={() => setEditing({ classroom: null, values: { branchId: "", gradeLevelId: "", name: "" } })}
        >
          {t("classrooms.add")}
        </button>
        {activeLevels.length === 0 && <span id="classroom-no-level" className="notice">{t("classrooms.noLevel")}</span>}
      </div>
      {editing && list.kind === "ready" && list.list.termId && (
        <ClassroomForm
          client={client}
          termId={list.list.termId}
          classroom={editing.classroom}
          initial={editing.values}
          branches={meta.branches.filter((b) => b.isActive)}
          levels={meta.levels.filter((l) => l.isActive || l.id === editing.classroom?.gradeLevelId)}
          onCancel={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            refresh();
          }}
        />
      )}
      {actionError && <p className="form-error" role="alert">{actionError}</p>}
      {list.kind === "loading" && <p className="notice" aria-busy="true">{t("admin.loading")}</p>}
      {list.kind === "error" && (
        <>
          <p className="form-error" role="alert">{list.message}</p>
          <div className="actions">
            <button type="button" onClick={refresh}>{t("admin.retry")}</button>
          </div>
        </>
      )}
      {list.kind === "ready" && (list.list.items.length === 0 ? (
        <p>{t("classrooms.empty")}</p>
      ) : (
        <ul className="admin-list">
          {list.list.items.map((classroom) => (
            <li key={classroom.id} className="admin-row">
              <span>
                <span className="notice">{classroom.branchName} · {classroom.gradeLevelName} · </span>
                <strong>{classroom.name}</strong>
                {!classroom.isActive && <span className="badge"> {t("academic.inactive")}</span>}
                {!classroom.branchIsActive && <span className="badge"> {t("classrooms.branchInactive")}</span>}
              </span>
              <span className="actions">
                <button
                  type="button"
                  className="secondary"
                  onClick={() => setEditing({ classroom, values: { branchId: classroom.branchId, gradeLevelId: classroom.gradeLevelId, name: classroom.name } })}
                >
                  {t("academic.edit")}
                </button>
                {classroom.isActive ? (
                  <button type="button" className="secondary" onClick={() => setConfirm(classroom)}>{t("academic.deactivate")}</button>
                ) : (
                  <button type="button" className="secondary" onClick={() => setActive(classroom, true)}>{t("academic.activate")}</button>
                )}
              </span>
            </li>
          ))}
        </ul>
      ))}
      {confirm && <ConfirmDialog message={t("classrooms.confirmDeactivate")} onCancel={() => setConfirm(null)} onConfirm={() => setActive(confirm, false)} />}
    </div>
  );
}

interface FormProps {
  client: Client;
  termId: string;
  classroom: ClassroomAdminView | null;
  initial: ClassroomFormValues;
  branches: BranchAdminView[];
  levels: GradeLevelAdminView[];
  onCancel: () => void;
  onSaved: () => void;
}

function ClassroomForm({ client, termId, classroom, initial, branches, levels, onCancel, onSaved }: FormProps) {
  const [values, setValues] = useState(initial);
  const [errors, setErrors] = useState<ReturnType<typeof validateClassroomForm>>({});
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState<string | null>(null);
  const creating = classroom === null;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const found = validateClassroomForm(values, creating);
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSaving(true);
    const outcome = creating
      ? await call(client.POST("/api/admin/classrooms", { body: { termId, branchId: values.branchId, gradeLevelId: values.gradeLevelId, name: values.name.trim() } }))
      : await call(client.PUT("/api/admin/classrooms/{id}", {
          params: { path: { id: classroom.id } },
          body: { termId: null, branchId: null, gradeLevelId: values.gradeLevelId, name: values.name.trim() },
        }));
    setSaving(false);
    if (outcome.ok) {
      onSaved();
    } else {
      setServerError(failureText(outcome));
    }
  };

  const aria = (name: keyof ClassroomFormValues) => ({
    id: `classroom-form-${name}`,
    "aria-invalid": errors[name] ? true : undefined,
    "aria-describedby": errors[name] ? `classroom-form-${name}-error` : undefined,
  });
  const errorText = (name: keyof ClassroomFormValues) => {
    const error = errors[name];
    return error && <p id={`classroom-form-${name}-error`} className="field-error" role="alert">{academicFieldText(error, "classrooms")}</p>;
  };

  return (
    <form className="card auth-form" onSubmit={submit} noValidate aria-label={creating ? t("classrooms.add") : t("academic.edit")}>
      {creating && (
        <>
          <label htmlFor="classroom-form-branchId">{t("classrooms.branch")}</label>
          <select {...aria("branchId")} value={values.branchId} onChange={(e) => setValues({ ...values, branchId: e.target.value })}>
            <option value="">{t("academic.choose")}</option>
            {branches.map((branch) => (
              <option key={branch.id} value={branch.id}>{branch.name}</option>
            ))}
          </select>
          {errorText("branchId")}
        </>
      )}
      <label htmlFor="classroom-form-gradeLevelId">{t("classrooms.level")}</label>
      <select {...aria("gradeLevelId")} value={values.gradeLevelId} onChange={(e) => setValues({ ...values, gradeLevelId: e.target.value })}>
        <option value="">{t("academic.choose")}</option>
        {levels.map((level) => (
          <option key={level.id} value={level.id}>{level.name}</option>
        ))}
      </select>
      {errorText("gradeLevelId")}
      <label htmlFor="classroom-form-name">{t("classrooms.name")}</label>
      <input {...aria("name")} value={values.name} onChange={(e) => setValues({ ...values, name: e.target.value })} />
      {errorText("name")}
      {serverError && <p className="form-error" role="alert">{serverError}</p>}
      <div className="actions">
        <button type="submit" disabled={saving} aria-busy={saving}>{saving ? t("academic.saving") : t("academic.save")}</button>
        <button type="button" className="secondary" onClick={onCancel}>{t("academic.cancel")}</button>
      </div>
    </form>
  );
}
