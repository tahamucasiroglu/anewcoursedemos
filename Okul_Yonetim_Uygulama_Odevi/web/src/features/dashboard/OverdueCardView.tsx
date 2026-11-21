import { useCallback } from "react";
import { dayText, moneyText, type AuthSession, type OverdueCard } from "@okul/api-client";
import { t } from "../../i18n/t";
import { call } from "../admin/adminApi";
import { CardFrame } from "./CardFrame";
import { fill } from "./dashboardText";
import { useLoad } from "./useLoad";

interface Props {
  client: AuthSession["client"];
  branchId: string;
  refresh: number;
}

/**
 * Geciken ödemeler kartı (spec 0017 gereksinim 4): "Geciken ödeme yok" ya da "N öğrencinin X TL ödemesi gecikti", en eski vadeden ilk 10
 * öğrenci tablosu (başlık hücreli; dar ekranda tablo kendi içinde kayar) ve "Tümünü gör" → borç raporu "yalnızca gecikenler".
 */
export function OverdueCardView({ client, branchId, refresh }: Props) {
  const load = useCallback(
    () => call(client.GET("/api/dashboard/summary/overdue", { params: { query: branchId ? { branchId } : {} } })),
    [client, branchId],
  );
  const { state, retry } = useLoad<OverdueCard>(load, refresh);
  return (
    <CardFrame id="card-overdue" title={t("dashboard.overdue.title")} state={state} onRetry={retry}>
      {(card) => (Number(card.studentCount) === 0 ? <p>{t("dashboard.overdue.none")}</p> : <OverdueBody card={card} />)}
    </CardFrame>
  );
}

function OverdueBody({ card }: { card: OverdueCard }) {
  return (
    <>
      <p className="summary-figure">{fill("dashboard.overdue.summary", { n: card.studentCount, amount: moneyText(card.overdueAmount) })}</p>
      <div className="table-scroll">
        <table className="data-table">
          <caption>{t("dashboard.overdue.caption")}</caption>
          <thead>
            <tr>
              <th scope="col">{t("dashboard.col.student")}</th>
              <th scope="col">{t("dashboard.col.classroom")}</th>
              <th scope="col" className="num">{t("dashboard.col.overdue")}</th>
              <th scope="col">{t("dashboard.col.oldestDue")}</th>
              <th scope="col" className="num">{t("dashboard.col.days")}</th>
            </tr>
          </thead>
          <tbody>
            {card.top.map((row) => (
              <tr key={row.studentId}>
                <th scope="row">{row.fullName}</th>
                <td>{row.classroomName ? `${row.branchName} / ${row.classroomName}` : row.branchName}</td>
                <td className="num">{moneyText(row.overdueAmount)}</td>
                <td>{dayText(row.oldestDue)}</td>
                <td className="num">{fill("dashboard.overdue.days", { n: row.daysOverdue })}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="actions">
        <a href="#/panel/raporlar?rapor=borc&gecikenler=1">{t("dashboard.overdue.seeAll")}</a>
      </div>
    </>
  );
}
