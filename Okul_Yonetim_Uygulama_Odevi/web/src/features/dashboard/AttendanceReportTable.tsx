import type { AttendanceReportPage } from "@okul/api-client";
import { t } from "../../i18n/t";
import { fill } from "./dashboardText";

/** Devamsızlık raporu tablosu (spec 0017 gereksinim 7): öğrenci, okul no, şube / sınıf, yok ve geç gün; altta bütün sonucun toplamı. */
export function AttendanceReportTable({ report }: { report: AttendanceReportPage }) {
  return (
    <div className="table-scroll">
      <table className="data-table">
        <caption>{t("dashboard.reports.attendanceCaption")}</caption>
        <thead>
          <tr>
            <th scope="col">{t("dashboard.col.student")}</th>
            <th scope="col">{t("dashboard.col.schoolNumber")}</th>
            <th scope="col">{t("dashboard.col.classroom")}</th>
            <th scope="col" className="num">{t("dashboard.col.absent")}</th>
            <th scope="col" className="num">{t("dashboard.col.late")}</th>
          </tr>
        </thead>
        <tbody>
          {report.items.map((row) => (
            <tr key={row.studentId}>
              <th scope="row">{row.fullName}</th>
              <td>{row.schoolNumber ?? ""}</td>
              <td>{row.classroomName ? `${row.branchName} / ${row.classroomName}` : row.branchName}</td>
              <td className="num">{row.absentDays}</td>
              <td className="num">{row.lateDays}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th scope="row" colSpan={3}>{fill("dashboard.reports.studentTotals", { n: report.totalCount })}</th>
            <td className="num">{report.totals.absentDays}</td>
            <td className="num">{report.totals.lateDays}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}
