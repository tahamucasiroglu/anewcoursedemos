import { t } from "../../i18n/t";
import { fill } from "./dashboardText";

interface Props {
  page: number;
  pageSize: number;
  totalCount: number;
  onPage: (page: number) => void;
}

/** Rapor tablosunun sayfa düğmeleri (spec 0017 gereksinim 6: 50'lik sayfa); tek sayfada görünmez. */
export function Pager({ page, pageSize, totalCount, onPage }: Props) {
  const pages = Math.max(1, Math.ceil(totalCount / pageSize));
  if (pages <= 1) {
    return null;
  }

  return (
    <div className="actions pager">
      <button type="button" className="secondary" disabled={page <= 1} onClick={() => onPage(page - 1)}>{t("dashboard.reports.prev")}</button>
      <span>{fill("dashboard.reports.page", { page, pages })}</span>
      <button type="button" className="secondary" disabled={page >= pages} onClick={() => onPage(page + 1)}>{t("dashboard.reports.next")}</button>
    </div>
  );
}
