// Okul takvimi gösterim yardımcıları (spec 0008): web ve mobil aynı tarih metnini ve tür anahtarını kullanır.
// Metinler istemcinin tr.json'undadır; burada yalnızca anahtar ve sayısal tarih biçimi üretilir.
import type { components } from "../schema";

export type SchoolCalendarView = components["schemas"]["SchoolCalendarView"];
export type ClassroomSummary = components["schemas"]["ClassroomSummary"];

/** Tür metninin anahtarı (tr.json: calendar.kind.holiday …); bilinmeyen tür "other". */
export type CalendarKindKey = "holiday" | "exam" | "event" | "other";

export function calendarKindKey(kind: string): CalendarKindKey {
  switch (kind) {
    case "Holiday":
      return "holiday";
    case "Exam":
      return "exam";
    case "Event":
      return "event";
    default:
      return "other";
  }
}

/** "2026-10-29" → "29.10.2026" (Türkiye'de alışılan gün.ay.yıl). */
export function formatDay(day: string): string {
  const [year, month, date] = day.split("-");
  return year && month && date ? `${date}.${month}.${year}` : day;
}

/** Tek günlük kayıt tek tarih; birden çok gün "başlangıç – bitiş". */
export function dateRangeText(startDate: string, endDate: string): string {
  return startDate === endDate ? formatDay(startDate) : `${formatDay(startDate)} – ${formatDay(endDate)}`;
}
