// Yoklama ekranının ortak mantığı (spec 0010): işaretler (öğrenci → Var/Yok/Geç), sayılar, "Kaydet (3 yok, 1 geç)" düğmesi ve
// kayıt sonucu metni. Metin şablonları ekranın kendi tr.json'undan gelir ({n}, {present} gibi yer tutucularla); burada
// yalnızca hangi parçanın, hangi sayıyla, hangi sırayla yazılacağı belirlenir — web ve mobil aynı sonucu gösterir.
import type { components } from "../schema";

export type MyClassroomRow = components["schemas"]["MyClassroomRow"];
export type AttendanceSheetView = components["schemas"]["AttendanceSheetView"];
export type AttendanceStudentRow = components["schemas"]["AttendanceStudentRow"];
export type AttendanceSaveResult = components["schemas"]["AttendanceSaveResult"];
export type ClassroomTeachersView = components["schemas"]["ClassroomTeachersView"];
export type TeacherOption = components["schemas"]["TeacherOption"];

export type AttendanceStatus = "Present" | "Absent" | "Late";
export const ATTENDANCE_STATUSES: readonly AttendanceStatus[] = ["Present", "Absent", "Late"];
export type AttendanceMarks = Record<string, AttendanceStatus>;
export interface AttendanceCounts { present: number; absent: number; late: number }

function asStatus(value: string): AttendanceStatus {
  return value === "Absent" || value === "Late" ? value : "Present";
}

/** Listenin kayıtlı (ya da varsayılan "Var") durumları. */
export function initialMarks(students: readonly AttendanceStudentRow[]): AttendanceMarks {
  return Object.fromEntries(students.map((s) => [s.studentId, asStatus(s.status)]));
}

export function setMark(marks: AttendanceMarks, studentId: string, status: AttendanceStatus): AttendanceMarks {
  return { ...marks, [studentId]: status };
}

export function countMarks(marks: AttendanceMarks): AttendanceCounts {
  const counts: AttendanceCounts = { present: 0, absent: 0, late: 0 };
  for (const status of Object.values(marks)) {
    if (status === "Absent") {
      counts.absent++;
    } else if (status === "Late") {
      counts.late++;
    } else {
      counts.present++;
    }
  }

  return counts;
}

/**
 * Kaydetme isteğinin gövdesi: gün (sunucunun verdiği bugün), her öğrencinin durumu ve ekranın açıldığı yoklama sürümü
 * (listedeki `version`, kayıttan sonra sonucun `version`'u). Arada başkası kaydettiyse sunucu 409 döner; ekran
 * listeyi yeniler (0010 review F-1).
 */
export function saveBody(day: string, marks: AttendanceMarks, version: number | string) {
  return { day, entries: Object.entries(marks).map(([studentId, status]) => ({ studentId, status })), version: Number(version) };
}

/** Kaydetme 409 döndü mü (ekran eski: başkası arada kaydetti)? */
export function isStaleSave(outcome: { ok: boolean; unreachable?: boolean; status?: number }): boolean {
  return !outcome.ok && outcome.unreachable === false && outcome.status === 409;
}

/** "{n}" gibi yer tutucuları doldurur (tek geçiş; değerdeki süslü parantez yeniden açılmaz). */
export function fillText(template: string, values: Record<string, string | number>): string {
  return template.replace(/\{(\w+)\}/g, (whole, name: string) => (name in values ? String(values[name]) : whole));
}

export interface SaveButtonTexts { plain: string; withCounts: string; absent: string; late: string; separator: string }

/** "Kaydet" ya da "Kaydet (3 yok, 1 geç)": yalnızca sıfır olmayan yok/geç sayıları, bu sırayla. */
export function saveButtonText(counts: AttendanceCounts, texts: SaveButtonTexts): string {
  const parts = [
    counts.absent > 0 ? fillText(texts.absent, { n: counts.absent }) : null,
    counts.late > 0 ? fillText(texts.late, { n: counts.late }) : null,
  ].filter((p): p is string => p !== null);
  return parts.length === 0 ? texts.plain : fillText(texts.withCounts, { parts: parts.join(texts.separator) });
}

export interface ResultTexts { saved: string; withoutGuardian: string }

/** "Kaydedildi — 36 var, 3 yok, 1 geç. 5 veliye bildirim gitti." (+ " 2 öğrencinin velisi kayıtlı değil."). */
export function saveResultText(result: AttendanceSaveResult, texts: ResultTexts): string {
  const saved = fillText(texts.saved, {
    present: Number(result.present),
    absent: Number(result.absent),
    late: Number(result.late),
    notified: Number(result.notifiedGuardians),
  });
  const missing = Number(result.studentsWithoutGuardian);
  return missing > 0 ? `${saved} ${fillText(texts.withoutGuardian, { n: missing })}` : saved;
}
