import { useEffect, useState, type FormEvent } from "react";
import { moneyText, type AuthSession, type StudentAccountPage } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { failureText } from "../admin/adminText";
import { fill } from "./billingText";

type Client = AuthSession["client"];
type ListState = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; page: StudentAccountPage };
interface Filter { search: string; branchId: string; overdueOnly: boolean; page: number }

/**
 * Cari listesi (spec 0015 gereksinim 8): planı olan öğrenciler, şube/sınıf, net, ödenen, kalan, geciken; ad araması, şube ve "yalnızca
 * gecikenler" süzgeci, 25'lik sayfa, altta süzülmüş bütün satırların toplamı. Durumlar: yükleniyor / boş ("Henüz ödeme planı olan
 * öğrenci yok") / dolu / hata + "Tekrar dene". Tablo başlık hücreli; dar ekranda tablo kendi içinde kayar.
 */
export function StudentAccountsTab({ client }: { client: Client }) {
  const [filter, setFilter] = useState<Filter>({ search: "", branchId: "", overdueOnly: false, page: 1 });
  const [draft, setDraft] = useState<Filter>(filter);
  const [list, setList] = useState<ListState>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    const query = {
      page: filter.page,
      overdueOnly: filter.overdueOnly,
      ...(filter.branchId ? { branchId: filter.branchId } : {}),
      ...(filter.search.trim() ? { search: filter.search.trim() } : {}),
    };
    call(client.GET("/api/billing/students", { params: { query } })).then((outcome) => {
      if (active) {
        setList(outcome.ok ? { kind: "ready", page: outcome.data } : { kind: "error", message: failureText(outcome) });
      }
    });
    return () => {
      active = false;
    };
  }, [client, filter, reload]);

  const apply = (event: FormEvent) => {
    event.preventDefault();
    setList({ kind: "loading" });
    setFilter({ ...draft, page: 1 });
  };

  const goTo = (page: number) => {
    setList({ kind: "loading" });
    setFilter({ ...filter, page });
  };

  const branches = list.kind === "ready" ? list.page.branches : [];
  return (
    <div className="admin-section">
      <form className="filters" onSubmit={apply} aria-label={t("billing.students.apply")}>
        <label htmlFor="billing-search">{t("billing.students.search")}</label>
        <input id="billing-search" value={draft.search} onChange={(e) => setDraft({ ...draft, search: e.target.value })} />
        <label htmlFor="billing-branch">{t("billing.students.branch")}</label>
        <select id="billing-branch" value={draft.branchId} onChange={(e) => setDraft({ ...draft, branchId: e.target.value })}>
          <option value="">{t("billing.students.allBranches")}</option>
          {branches.map((b) => (
            <option key={b.id} value={b.id}>{b.name}</option>
          ))}
        </select>
        <span />
        <label className="check">
          <input type="checkbox" checked={draft.overdueOnly} onChange={(e) => setDraft({ ...draft, overdueOnly: e.target.checked })} /> {t("billing.students.overdueOnly")}
        </label>
        <span />
        <div className="actions">
          <button type="submit">{t("billing.students.apply")}</button>
        </div>
      </form>
      {list.kind === "loading" && <p className="notice" aria-busy="true">{t("billing.loading")}</p>}
      {list.kind === "error" && (
        <div>
          <p className="form-error" role="alert">{list.message}</p>
          <div className="actions">
            <button type="button" onClick={() => { setList({ kind: "loading" }); setReload((n) => n + 1); }}>{t("billing.retry")}</button>
          </div>
        </div>
      )}
      {list.kind === "ready" && Number(list.page.totalCount) === 0 && <p>{t("billing.students.empty")}</p>}
      {list.kind === "ready" && Number(list.page.totalCount) > 0 && <AccountsTable page={list.page} onPage={goTo} />}
    </div>
  );
}

function AccountsTable({ page, onPage }: { page: StudentAccountPage; onPage: (page: number) => void }) {
  const current = Number(page.page);
  const pages = Math.max(1, Math.ceil(Number(page.totalCount) / Number(page.pageSize)));
  return (
    <>
      <div className="table-scroll">
        <table className="data-table">
          <caption>{t("billing.students.caption")}</caption>
          <thead>
            <tr>
              <th scope="col">{t("billing.col.student")}</th>
              <th scope="col">{t("billing.col.branch")}</th>
              <th scope="col" className="num">{t("billing.col.net")}</th>
              <th scope="col" className="num">{t("billing.col.paid")}</th>
              <th scope="col" className="num">{t("billing.col.remaining")}</th>
              <th scope="col" className="num">{t("billing.col.overdue")}</th>
            </tr>
          </thead>
          <tbody>
            {page.items.map((row) => (
              <tr key={row.studentId}>
                <th scope="row">
                  <a href={`#/tahsilat/ogrenci/${row.studentId}`}>{row.fullName}</a>
                  {!row.isActive && <span className="badge"> {t("billing.students.left")}</span>}
                </th>
                <td>{row.classroomName ? `${row.branchName} / ${row.classroomName}` : row.branchName}</td>
                <td className="num">{moneyText(row.totals.net)}</td>
                <td className="num">{moneyText(row.totals.paid)}</td>
                <td className="num">{moneyText(row.totals.remaining)}</td>
                <td className="num">{moneyText(row.totals.overdue)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <th scope="row" colSpan={2}>{fill("billing.students.totals", { n: page.totalCount })}</th>
              <td className="num">{moneyText(page.totals.net)}</td>
              <td className="num">{moneyText(page.totals.paid)}</td>
              <td className="num">{moneyText(page.totals.remaining)}</td>
              <td className="num">{moneyText(page.totals.overdue)}</td>
            </tr>
          </tfoot>
        </table>
      </div>
      {pages > 1 && (
        <div className="actions pager">
          <button type="button" className="secondary" disabled={current <= 1} onClick={() => onPage(current - 1)}>{t("billing.students.prev")}</button>
          <span>{fill("billing.students.page", { page: current, pages })}</span>
          <button type="button" className="secondary" disabled={current >= pages} onClick={() => onPage(current + 1)}>{t("billing.students.next")}</button>
        </div>
      )}
    </>
  );
}
