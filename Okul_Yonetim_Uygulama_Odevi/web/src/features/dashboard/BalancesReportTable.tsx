import { dayText, moneyText, type BalancesReportPage } from "@okul/api-client";
import { t } from "../../i18n/t";
import { fill } from "./dashboardText";

/** Borç/gecikme raporu tablosu (spec 0017 gereksinim 9): öğrenci, şube / sınıf, plan, net, ödenen, kalan, geciken, en eski geciken vade; altta toplamlar. */
export function BalancesReportTable({ report }: { report: BalancesReportPage }) {
  return (
    <div className="table-scroll">
      <table className="data-table">
        <caption>{fill("dashboard.reports.balancesCaption", { date: dayText(report.today) })}</caption>
        <thead>
          <tr>
            <th scope="col">{t("dashboard.col.student")}</th>
            <th scope="col">{t("dashboard.col.classroom")}</th>
            <th scope="col">{t("dashboard.col.plan")}</th>
            <th scope="col" className="num">{t("dashboard.col.net")}</th>
            <th scope="col" className="num">{t("dashboard.col.paid")}</th>
            <th scope="col" className="num">{t("dashboard.col.remaining")}</th>
            <th scope="col" className="num">{t("dashboard.col.overdue")}</th>
            <th scope="col">{t("dashboard.col.oldestDue")}</th>
          </tr>
        </thead>
        <tbody>
          {report.items.map((row) => (
            <tr key={row.studentId}>
              <th scope="row">{row.fullName}</th>
              <td>{row.classroomName ? `${row.branchName} / ${row.classroomName}` : row.branchName}</td>
              <td>{row.planNames}</td>
              <td className="num">{moneyText(row.net)}</td>
              <td className="num">{moneyText(row.paid)}</td>
              <td className="num">{moneyText(row.remaining)}</td>
              <td className="num">{moneyText(row.overdue)}</td>
              <td>{row.oldestOverdueDue ? dayText(row.oldestOverdueDue) : t("dashboard.reports.noOverdue")}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th scope="row" colSpan={3}>{fill("dashboard.reports.studentTotals", { n: report.totalCount })}</th>
            <td className="num">{moneyText(report.totals.net)}</td>
            <td className="num">{moneyText(report.totals.paid)}</td>
            <td className="num">{moneyText(report.totals.remaining)}</td>
            <td className="num">{moneyText(report.totals.overdue)}</td>
            <td />
          </tr>
        </tfoot>
      </table>
    </div>
  );
}
