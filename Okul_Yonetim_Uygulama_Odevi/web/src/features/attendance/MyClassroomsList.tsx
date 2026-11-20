import type { MyClassroomRow } from "@okul/api-client";
import { t } from "../../i18n/t";

interface Props {
  rows: MyClassroomRow[];
  onOpen: (id: string) => void;
}

/** Sınıflarım listesi (spec 0010): sınıf, şube ve bugünkü durum ("Bugün alındı" / "Bugün alınmadı"); boşsa açık mesaj. */
export function MyClassroomsList({ rows, onOpen }: Props) {
  if (rows.length === 0) {
    return <p>{t("attendance.noClassrooms")}</p>;
  }

  return (
    <ul className="admin-list">
      {rows.map((row) => {
        const status = row.takenToday ? t("attendance.taken") : t("attendance.notTaken");
        return (
          <li key={row.id} className="admin-row">
            <span>
              <strong>{row.name}</strong>
              <span className="notice"> · {row.branchName} · {status}</span>
            </span>
            <button type="button" className="secondary" aria-label={`${row.name}, ${row.branchName}, ${status}, ${t("attendance.open")}`} onClick={() => onOpen(row.id)}>
              {t("attendance.open")}
            </button>
          </li>
        );
      })}
    </ul>
  );
}
