import { useCallback } from "react";
import type { AttendanceCard, AuthSession } from "@okul/api-client";
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
 * Devamsızlık kartı (spec 0017 gereksinim 3): "Bugün okul günü değil" / "Henüz sınıf yok" / alınan ve alınmayan sınıf sayısı, alınmayanların
 * listesi (şube / sınıf — öğretmen), yok ve geç sayısı metinle, yok öğrencilerin listesi; "Devamsızlık raporu" bağlantısı.
 */
export function AttendanceCardView({ client, branchId, refresh }: Props) {
  const load = useCallback(
    () => call(client.GET("/api/dashboard/summary/attendance", { params: { query: branchId ? { branchId } : {} } })),
    [client, branchId],
  );
  const { state, retry } = useLoad<AttendanceCard>(load, refresh);
  return (
    <CardFrame id="card-attendance" title={t("dashboard.attendance.title")} state={state} onRetry={retry}>
      {(card) => <AttendanceBody card={card} />}
    </CardFrame>
  );
}

function AttendanceBody({ card }: { card: AttendanceCard }) {
  if (!card.isSchoolDay) {
    return <p>{t("dashboard.attendance.notSchoolDay")}</p>;
  }

  if (Number(card.classroomCount) === 0) {
    return <p>{t("dashboard.attendance.noClassrooms")}</p>;
  }

  return (
    <>
      <p className="summary-figure">{fill("dashboard.attendance.taken", { taken: card.takenCount, total: card.classroomCount })}</p>
      <p className="summary-figure">{fill("dashboard.attendance.counts", { absent: card.absentCount, late: card.lateCount })}</p>
      <h4>{fill("dashboard.attendance.notTakenTitle", { n: card.notTakenCount })}</h4>
      {card.notTaken.length === 0 ? (
        <p>{t("dashboard.attendance.allTaken")}</p>
      ) : (
        <ul className="summary-list">
          {card.notTaken.map((row) => (
            <li key={row.classroomId}>
              {fill("dashboard.attendance.notTakenRow", {
                branch: row.branchName,
                classroom: row.classroomName,
                teachers: row.teacherNames.length > 0 ? row.teacherNames.join(", ") : t("dashboard.attendance.noTeacher"),
              })}
            </li>
          ))}
        </ul>
      )}
      <h4>{t("dashboard.attendance.absentTitle")}</h4>
      {card.absentStudents.length === 0 ? (
        <p>{t("dashboard.attendance.noAbsent")}</p>
      ) : (
        <ul className="summary-list">
          {card.absentStudents.map((row) => (
            <li key={row.studentId}>{`${row.fullName} — ${row.branchName} / ${row.classroomName}`}</li>
          ))}
        </ul>
      )}
      <div className="actions">
        <a href="#/panel/raporlar?rapor=devamsizlik">{t("dashboard.attendance.seeAll")}</a>
      </div>
    </>
  );
}
