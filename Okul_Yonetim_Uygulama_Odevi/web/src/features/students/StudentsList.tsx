import { useEffect, useState } from "react";
import type { AuthSession, BranchAdminView, ClassroomSummary, StudentPage } from "@okul/api-client";
import { t } from "../../i18n/t";
import { failureText } from "../admin/adminText";
import { call } from "./studentsApi";

type Client = AuthSession["client"];
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; page: StudentPage };
interface Filters { search: string; branchId: string; classroomId: string; status: string; page: number }
const START: Filters = { search: "", branchId: "", classroomId: "", status: "", page: 1 };

interface Props {
  client: Client;
  onAdd: () => void;
  onOpen: (id: string) => void;
}

/**
 * Öğrenci listesi (spec 0009 gereksinim 4): ad / okul numarası araması, şube, sınıf ve durum süzgeci, sayfalama (25);
 * durumlar: yükleniyor, boş ("Henüz öğrenci yok" / aramada "Aramaya uyan öğrenci yok"), dolu, hata + tekrar dene. Satırda
 * ad-soyad, okul numarası, şube, sınıf, veli sayısı ve "Ayrıldı" etiketi; sağlık bilgisi yok.
 */
export function StudentsList({ client, onAdd, onOpen }: Props) {
  const [filters, setFilters] = useState(START);
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [branches, setBranches] = useState<BranchAdminView[]>([]);
  const [classrooms, setClassrooms] = useState<ClassroomSummary[]>([]);

  useEffect(() => {
    let active = true;
    Promise.all([call(client.GET("/api/admin/branches")), call(client.GET("/api/classrooms"))]).then(([b, c]) => {
      if (active) {
        setBranches(b.ok ? b.data : []);
        setClassrooms(c.ok ? c.data : []);
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
      branchId: filters.branchId || undefined,
      classroomId: filters.classroomId || undefined,
      status: filters.status || undefined,
      page: filters.page,
    };
    call(client.GET("/api/admin/students", { params: { query } })).then((outcome) => {
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

  const goTo = (pageNo: number) => {
    setList({ kind: "loading" });
    setFilters((current) => ({ ...current, page: pageNo }));
  };

  const filtering = filters.search.trim() !== "" || filters.branchId !== "" || filters.classroomId !== "" || filters.status !== "";
  const visibleClassrooms = classrooms.filter((c) => filters.branchId === "" || c.branchId === filters.branchId);
  const total = list.kind === "ready" ? Number(list.page.total) : 0;
  const pageSize = list.kind === "ready" ? Number(list.page.pageSize) : 25;
  const pages = Math.max(1, Math.ceil(total / pageSize));
  return (
    <div className="admin-section">
      <div className="actions">
        <button type="button" onClick={onAdd}>{t("students.add")}</button>
      </div>
      <div className="filters" role="search">
        <label htmlFor="student-search">{t("students.search")}</label>
        <input id="student-search" type="search" value={filters.search} onChange={(e) => change({ search: e.target.value })} />
        <label htmlFor="student-branch">{t("students.branchFilter")}</label>
        <select id="student-branch" value={filters.branchId} onChange={(e) => change({ branchId: e.target.value, classroomId: "" })}>
          <option value="">{t("students.all")}</option>
          {branches.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
        </select>
        <label htmlFor="student-classroom">{t("students.classroomFilter")}</label>
        <select id="student-classroom" value={filters.classroomId} onChange={(e) => change({ classroomId: e.target.value })}>
          <option value="">{t("students.all")}</option>
          {visibleClassrooms.map((c) => <option key={c.id} value={c.id}>{`${c.branchName} · ${c.name}`}</option>)}
        </select>
        <label htmlFor="student-status">{t("students.statusFilter")}</label>
        <select id="student-status" value={filters.status} onChange={(e) => change({ status: e.target.value })}>
          <option value="">{t("students.all")}</option>
          <option value="active">{t("students.active")}</option>
          <option value="left">{t("students.left")}</option>
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
        <p>{filtering ? t("students.noMatch") : t("students.empty")}</p>
      ) : (
        <>
          <ul className="admin-list">
            {list.page.items.map((s) => (
              <li key={s.id} className="admin-row">
                <span>
                  <strong>{s.fullName}</strong>
                  {s.schoolNumber && <span className="notice"> · {s.schoolNumber}</span>}
                  {!s.isActive && <span className="badge"> {t("students.leftBadge")}</span>}
                  <br />
                  <span className="notice">
                    {`${s.branchName} · ${s.classroomName ?? t("students.noClassroom")} · ${Number(s.guardianCount)} ${t("students.guardianCount")}`}
                  </span>
                </span>
                <button type="button" className="secondary" onClick={() => onOpen(s.id)} aria-label={`${s.fullName} — ${t("students.open")}`}>
                  {t("students.open")}
                </button>
              </li>
            ))}
          </ul>
          {pages > 1 && (
            <nav className="actions" aria-label={t("students.page")}>
              <button type="button" className="secondary" disabled={filters.page <= 1} onClick={() => goTo(filters.page - 1)}>{t("students.prev")}</button>
              <span>{t("students.page")} {filters.page} / {pages}</span>
              <button type="button" className="secondary" disabled={filters.page >= pages} onClick={() => goTo(filters.page + 1)}>{t("students.next")}</button>
            </nav>
          )}
        </>
      ))}
    </div>
  );
}
