import { useEffect, useState } from "react";
import { ROLE_NAMES, type AddMemberResult, type AuthSession, type BranchAdminView, type MemberPage } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "./adminApi";
import { failureText, roleText } from "./adminText";
import { MemberCard } from "./MemberCard";
import { MemberForm } from "./MemberForm";
import { TempPasswordResult } from "./TempPasswordResult";

type Client = AuthSession["client"];
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; page: MemberPage };
interface Filters { search: string; role: string; branchId: string; status: string; page: number }
const START: Filters = { search: "", role: "", branchId: "", status: "", page: 1 };

/**
 * Kullanıcılar sekmesi (spec 0005): arama (ad/e-posta), rol/şube/durum süzgeci, sayfalama (25), liste durumları
 * (yükleniyor / boş / dolu / hata + tekrar dene), "Kullanıcı ekle" → sonuç (geçici şifre bir kez ya da "zaten hesabı
 * var"), kişi kartı.
 */
export function MembersTab({ client }: { client: Client }) {
  const [filters, setFilters] = useState(START);
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [branches, setBranches] = useState<BranchAdminView[]>([]);
  const [reload, setReload] = useState(0);
  const [view, setView] = useState<{ kind: "list" } | { kind: "add" } | { kind: "added"; result: AddMemberResult } | { kind: "card"; userId: string }>({ kind: "list" });

  useEffect(() => {
    let active = true;
    call(client.GET("/api/admin/branches")).then((outcome) => {
      if (active && outcome.ok) {
        setBranches(outcome.data);
      }
    });
    return () => {
      active = false;
    };
  }, [client]);

  useEffect(() => {
    let active = true;
    const query = {
      search: filters.search.trim() || undefined,
      role: filters.role || undefined,
      branchId: filters.branchId || undefined,
      status: filters.status || undefined,
      page: filters.page,
    };
    call(client.GET("/api/admin/members", { params: { query } })).then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", page: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, filters, reload]);

  const change = (next: Partial<Filters>) => {
    setList({ kind: "loading" });
    setFilters((current) => ({ ...current, page: 1, ...next }));
  };

  const backToList = () => {
    setView({ kind: "list" });
    setList({ kind: "loading" });
    setReload((n) => n + 1);
  };

  if (view.kind === "add") {
    return <MemberForm client={client} branches={branches} onCancel={() => setView({ kind: "list" })} onAdded={(result) => setView({ kind: "added", result })} />;
  }

  if (view.kind === "added") {
    return view.result.temporaryPassword ? (
      <TempPasswordResult password={view.result.temporaryPassword} onDone={backToList} />
    ) : (
      <section className="card">
        <p role="status">{t("members.existingAccount")}</p>
        <div className="actions">
          <button type="button" onClick={backToList}>{t("temp.done")}</button>
        </div>
      </section>
    );
  }

  if (view.kind === "card") {
    return <MemberCard client={client} userId={view.userId} branches={branches} onBack={backToList} />;
  }

  const total = list.kind === "ready" ? Number(list.page.total) : 0;
  const pageSize = list.kind === "ready" ? Number(list.page.pageSize) : 25;
  const pages = Math.max(1, Math.ceil(total / pageSize));
  return (
    <div className="admin-section">
      <div className="actions">
        <button type="button" onClick={() => setView({ kind: "add" })}>{t("members.add")}</button>
      </div>
      <div className="filters" role="search">
        <label htmlFor="member-search">{t("members.search")}</label>
        <input id="member-search" type="search" value={filters.search} onChange={(e) => change({ search: e.target.value })} />
        <label htmlFor="member-role">{t("members.role")}</label>
        <select id="member-role" value={filters.role} onChange={(e) => change({ role: e.target.value })}>
          <option value="">{t("members.all")}</option>
          {ROLE_NAMES.map((role) => <option key={role} value={role}>{roleText(role)}</option>)}
        </select>
        <label htmlFor="member-branch">{t("members.branch")}</label>
        <select id="member-branch" value={filters.branchId} onChange={(e) => change({ branchId: e.target.value })}>
          <option value="">{t("members.all")}</option>
          {branches.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
        </select>
        <label htmlFor="member-status">{t("members.status")}</label>
        <select id="member-status" value={filters.status} onChange={(e) => change({ status: e.target.value })}>
          <option value="">{t("members.all")}</option>
          <option value="active">{t("members.active")}</option>
          <option value="inactive">{t("members.inactive")}</option>
        </select>
      </div>
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
        <p>{t("members.empty")}</p>
      ) : (
        <>
          <ul className="admin-list">
            {list.page.items.map((m) => (
              <li key={m.userId} className="admin-row">
                <span>
                  <strong>{m.fullName}</strong> <span className="notice">{m.email}</span>
                  {!m.isActive && <span className="badge"> {t("members.inactive")}</span>}
                  <br />
                  <span className="notice">
                    {m.roles.map((r) => `${roleText(r.role)}${r.branchIds.length ? ` (${r.branchIds.map((id) => branches.find((b) => b.id === id)?.name ?? "?").join(", ")})` : ""}`).join(" · ")}
                  </span>
                </span>
                <button type="button" className="secondary" onClick={() => setView({ kind: "card", userId: m.userId })} aria-label={`${m.fullName} — ${t("members.open")}`}>
                  {t("members.open")}
                </button>
              </li>
            ))}
          </ul>
          {pages > 1 && (
            <nav className="actions" aria-label={t("members.page")}>
              <button type="button" className="secondary" disabled={filters.page <= 1} onClick={() => { setList({ kind: "loading" }); setFilters({ ...filters, page: filters.page - 1 }); }}>{t("members.prev")}</button>
              <span>{t("members.page")} {filters.page} / {pages}</span>
              <button type="button" className="secondary" disabled={filters.page >= pages} onClick={() => { setList({ kind: "loading" }); setFilters({ ...filters, page: filters.page + 1 }); }}>{t("members.next")}</button>
            </nav>
          )}
        </>
      ))}
    </div>
  );
}
