import { dayText, moneyText, type PaymentsReportPage } from "@okul/api-client";
import { t } from "../../i18n/t";
import { methodText } from "../billing/billingText";
import { fill } from "./dashboardText";

/**
 * Tahsilat raporu tablosu (spec 0017 gereksinim 8): tarih, makbuz no, öğrenci, şube, tutar, yöntem, durum (Geçerli / İptal — metinle) ve
 * kaydeden; altta geçerli tahsilat toplamı ve yönteme göre toplamlar (iptaller toplama girmez).
 */
export function PaymentsReportTable({ report }: { report: PaymentsReportPage }) {
  return (
    <div className="table-scroll">
      <table className="data-table">
        <caption>{t("dashboard.reports.paymentsCaption")}</caption>
        <thead>
          <tr>
            <th scope="col">{t("dashboard.col.date")}</th>
            <th scope="col" className="num">{t("dashboard.col.receiptNo")}</th>
            <th scope="col">{t("dashboard.col.student")}</th>
            <th scope="col">{t("dashboard.col.branch")}</th>
            <th scope="col" className="num">{t("dashboard.col.amount")}</th>
            <th scope="col">{t("dashboard.col.method")}</th>
            <th scope="col">{t("dashboard.col.status")}</th>
            <th scope="col">{t("dashboard.col.recordedBy")}</th>
          </tr>
        </thead>
        <tbody>
          {report.items.map((row) => (
            <tr key={row.paymentId}>
              <td>{dayText(row.date)}</td>
              <td className="num">{row.receiptNo}</td>
              <th scope="row">{row.studentName}</th>
              <td>{row.branchName}</td>
              <td className="num">{moneyText(row.amount)}</td>
              <td>{methodText(row.method)}</td>
              <td>{t(row.cancelled ? "dashboard.status.cancelled" : "dashboard.status.valid")}</td>
              <td>{row.recordedByName}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th scope="row" colSpan={4}>{fill("dashboard.reports.validTotal", { n: report.totals.validCount })}</th>
            <td className="num">{moneyText(report.totals.validAmount)}</td>
            <td colSpan={3}>{fill("dashboard.reports.cancelledCount", { n: report.totals.cancelledCount })}</td>
          </tr>
          {report.totals.byMethod.map((m) => (
            <tr key={m.method}>
              <th scope="row" colSpan={4}>{fill("dashboard.reports.methodTotal", { method: methodText(m.method), n: m.count })}</th>
              <td className="num">{moneyText(m.amount)}</td>
              <td colSpan={3} />
            </tr>
          ))}
        </tfoot>
      </table>
    </div>
  );
}
