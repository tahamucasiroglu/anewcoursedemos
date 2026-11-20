import { useEffect, useState } from "react";
import { callApi, type AuthSession, type SchoolPage, type SchoolRow } from "@okul/api-client";
import { t } from "../../i18n/t";
import { ConfirmDialog } from "../admin/ConfirmDialog";
import { failureText } from "../admin/adminText";

type Client = AuthSession["client"];
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; page: SchoolPage };
const START = { search: "", status: "", page: 1 };

/**
 * Okul listesi (spec 0007): ad sırası; ad, açılış tarihi, durum, etkin şube ve kişi sayısı, kurucular. Arama (Türkçe harf
 * kuralı sunucuda), durum süzgeci, 25'lik sayfalar; yükleniyor / boş / hata + tekrar dene. Pasifleştir onaylı.
 */
export function SchoolsList({ client, onAdd }: { client: Client; onAdd: () => void }) {
  const [filters, setFilters] = useState(START);
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [confirm, setConfirm] = useState<SchoolRow | null>(null);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const query = { search: filters.search.trim() || undefined, status: filters.status || undefined, page: filters.page };
    callApi(client.GET("/api/platform/schools", { params: { query } })).then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", page: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, filters, reload]);

  const change = (patch: Partial<typeof START>) => {
    setList({ kind: "loading" });
    setFilters({ ...filters, ...patch, page: patch.page ?? 1 });
  };

  const setActive = async (school: SchoolRow, activate: boolean) => {
    setBusy(true);
    const path = activate ? "/api/platform/schools/{id}/activate" : "/api/platform/schools/{id}/deactivate";
    const outcome = await callApi(client.POST(path, { params: { path: { id: school.tenantId } } }));
    setBusy(false);
    setConfirm(null);
    if (outcome.ok) {
      setActionError(null);
      setList({ kind: "loading" });
      setReload((n) => n + 1);
    } else {
      setActionError(failureText(outcome));
    }
  };

  const pages = list.kind === "ready" ? Math.max(1, Math.ceil(Number(list.page.total) / Number(list.page.pageSize))) : 1;
  return (
    <div className="admin-section">
      <div className="actions">
        <button type="button" onClick={onAdd}>{t("platform.add")}</button>
      </div>
      <div className="filters" role="search">
        <label htmlFor="school-search">{t("platform.search")}</label>
        <input id="school-search" type="search" value={filters.search} onChange={(e) => change({ search: e.target.value })} />
        <label htmlFor="school-status">{t("members.status")}</label>
        <select id="school-status" value={filters.status} onChange={(e) => change({ status: e.target.value })}>
          <option value="">{t("members.all")}</option>
          <option value="active">{t("members.active")}</option>
          <option value="inactive">{t("members.inactive")}</option>
        </select>
      </div>
      {actionError && <p className="form-error" role="alert">{actionError}</p>}
      {list.kind === "loading" && <p className="notice" aria-busy="true">{t("admin.loading")}</p>}
      {list.kind === "error" && (
        <div>
          <p className="form-error" role="alert">{list.message}</p>
          <div className="actions">
            <button type="button" onClick={() => { setList({ kind: "loading" }); setReload((n) => n + 1); }}>{t("admin.retry")}</button>
          </div>
        </div>
      )}
      {list.kind === "ready" && (list.page.items.length === 0 ? (
        <p>{filters.search || filters.status ? t("platform.noMatch") : t("platform.empty")}</p>
      ) : (
        <>
          <ul className="admin-list">
            {list.page.items.map((school) => (
              <li key={school.tenantId} className="admin-row">
                <span>
                  <strong>{school.name}</strong>
                  {!school.isActive && <span className="badge"> {t("platform.inactive")}</span>}
                  <br />
                  <span className="notice">
                    {t("platform.opened")} {new Date(school.createdAt).toLocaleDateString("tr-TR")} · {t("platform.branches")} {school.activeBranchCount} · {t("platform.members")} {school.activeMemberCount}
                  </span>
                  <br />
                  <span className="notice">
                    {t("platform.founders")} {school.founders.map((f) => `${f.fullName} (${f.email})`).join(", ") || "—"}
                  </span>
                </span>
                {school.isActive ? (
                  <button type="button" className="secondary" onClick={() => setConfirm(school)} aria-label={`${school.name} — ${t("platform.deactivate")}`}>
                    {t("platform.deactivate")}
                  </button>
                ) : (
                  <button type="button" className="secondary" disabled={busy} onClick={() => setActive(school, true)} aria-label={`${school.name} — ${t("platform.activate")}`}>
                    {t("platform.activate")}
                  </button>
                )}
              </li>
            ))}
          </ul>
          {pages > 1 && (
            <nav className="actions" aria-label={t("members.page")}>
              <button type="button" className="secondary" disabled={filters.page <= 1} onClick={() => change({ page: filters.page - 1 })}>{t("members.prev")}</button>
              <span>{t("members.page")} {filters.page} / {pages}</span>
              <button type="button" className="secondary" disabled={filters.page >= pages} onClick={() => change({ page: filters.page + 1 })}>{t("members.next")}</button>
            </nav>
          )}
        </>
      ))}
      {confirm && <ConfirmDialog message={t("platform.confirmDeactivate")} busy={busy} onCancel={() => setConfirm(null)} onConfirm={() => setActive(confirm, false)} />}
    </div>
  );
}
