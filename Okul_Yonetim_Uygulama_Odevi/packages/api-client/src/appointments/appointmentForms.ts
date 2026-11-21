// Randevu formlarının alan kuralları ve gösterim yardımcıları (spec 0016): sunucudaki kurallarla aynı; hatalı alan varsa istek
// gönderilmez. Sonuç metin değil anahtardır; ekran metni kendi tr.json'undan alır. Kullanıcı günü "GG.AA.YYYY", saati "SS:DD"
// (Türkiye saati) yazar; istek günü "yyyy-MM-dd" taşır. Sunucudan gelen anlar UTC'dir; Türkiye günü ve saati sabit UTC+3 ile
// gösterilir (Türkiye 2016'dan beri yaz saati uygulamıyor — sunucunun yedeğiyle aynı).
import { addDays, parseDayInput, toDayInput } from "../academics/academicsForms";
import type { components } from "../schema";

export type TeacherSchedule = components["schemas"]["TeacherSchedule"];
export type ScheduleItem = components["schemas"]["ScheduleItem"];
export type AvailabilityOpened = components["schemas"]["AvailabilityOpened"];
export type BookableTeacher = components["schemas"]["BookableTeacher"];
export type OpenSlot = components["schemas"]["OpenSlot"];
export type AppointmentChanged = components["schemas"]["AppointmentChanged"];
export type MyAppointments = components["schemas"]["MyAppointments"];
export type MyAppointmentRow = components["schemas"]["MyAppointmentRow"];

export const APPOINTMENT_TOPIC_MAX = 300;
export const APPOINTMENT_REASON_MAX = 300;
export const APPOINTMENT_DURATIONS: readonly number[] = [15, 20, 30];
export const AVAILABILITY_MAX_DAYS = 60;
export const AVAILABILITY_MAX_HOURS = 8;
export const AVAILABILITY_STEP_MINUTES = 15;

/** Randevunun ekranda görünen durumu (sunucunun `state` alanı; "Süresi doldu" sunucuda hesaplanır). */
export type AppointmentStateKey = "Pending" | "Approved" | "Rejected" | "CancelledByGuardian" | "CancelledByTeacher" | "Expired";
/** Takvimdeki dilimin durumu. */
export type SlotStateKey = "Free" | "Pending" | "Approved" | "Expired";

export type AppointmentFieldError =
  | "dayRequired"
  | "dayInvalid"
  | "dayRange"
  | "startInvalid"
  | "endInvalid"
  | "startBeforeEnd"
  | "rangeTooLong"
  | "durationInvalid"
  | "topicRequired"
  | "topicTooLong"
  | "reasonTooLong";

export interface AvailabilityFormValues {
  /** "GG.AA.YYYY" */
  day: string;
  start: string;
  end: string;
  durationMinutes: number | null;
}

export type AvailabilityFormErrors = Partial<Record<keyof AvailabilityFormValues, AppointmentFieldError>>;

export interface DayGroup<T> {
  /** "yyyy-MM-dd" (Türkiye günü) */
  day: string;
  items: T[];
}

const TURKEY_OFFSET_MS = 3 * 3_600_000;
const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;
const WEEK: readonly string[] = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

function minutesOf(time: string): number | null {
  const match = TIME_PATTERN.exec(time);
  return match ? Number(match[1]) * 60 + Number(match[2]) : null;
}

/** Bugünden `offset` gün sonrası "GG.AA.YYYY" ("Bugün" / "Yarın" düğmeleri). */
export function dayInputFromToday(today: string, offset: number): string {
  return toDayInput(addDays(today, offset));
}

/** "SS:DD" biçiminde ve 15 dakikalık adımda mı. */
export function isAvailabilityTime(value: string): boolean {
  const minutes = minutesOf(value);
  return minutes !== null && minutes % AVAILABILITY_STEP_MINUTES === 0;
}

/** Müsaitlik formu: gün bugün–60 gün, saatler 15 dk adımında, başlangıç < bitiş, en fazla 8 saat, süre 15/20/30. */
export function validateAvailabilityForm(values: AvailabilityFormValues, today: string): AvailabilityFormErrors {
  const errors: AvailabilityFormErrors = {};
  if (values.day.trim() === "") {
    errors.day = "dayRequired";
  } else {
    const day = parseDayInput(values.day);
    if (day === null) {
      errors.day = "dayInvalid";
    } else if (day < today || day > addDays(today, AVAILABILITY_MAX_DAYS)) {
      errors.day = "dayRange";
    }
  }

  if (!isAvailabilityTime(values.start)) {
    errors.start = "startInvalid";
  }

  if (!isAvailabilityTime(values.end)) {
    errors.end = "endInvalid";
  }

  if (!errors.start && !errors.end) {
    const start = minutesOf(values.start) ?? 0;
    const end = minutesOf(values.end) ?? 0;
    if (start >= end) {
      errors.start = "startBeforeEnd";
    } else if (end - start > AVAILABILITY_MAX_HOURS * 60) {
      errors.end = "rangeTooLong";
    }
  }

  if (values.durationMinutes === null || !APPOINTMENT_DURATIONS.includes(values.durationMinutes)) {
    errors.durationMinutes = "durationInvalid";
  }

  return errors;
}

/** Aralığın kaç dilime bölüneceği (önizleme; sona kalan kısa parça sayılmaz). Geçersiz girişte 0. */
export function slotCountPreview(start: string, end: string, durationMinutes: number | null): number {
  const from = minutesOf(start);
  const to = minutesOf(end);
  if (from === null || to === null || durationMinutes === null || durationMinutes <= 0 || from >= to) {
    return 0;
  }

  return Math.floor((to - from) / durationMinutes);
}

/** Müsaitlik isteğinin gövdesi (gün "yyyy-MM-dd"). */
export function availabilityBody(values: AvailabilityFormValues) {
  return { day: parseDayInput(values.day), start: values.start, end: values.end, durationMinutes: values.durationMinutes };
}

/** Konu: boş olamaz, kırpılmış en fazla 300 karakter. */
export function validateTopic(text: string): AppointmentFieldError | undefined {
  const topic = text.trim();
  if (topic === "") {
    return "topicRequired";
  }

  return topic.length > APPOINTMENT_TOPIC_MAX ? "topicTooLong" : undefined;
}

/** Ret/iptal nedeni: isteğe bağlı, kırpılmış en fazla 300 karakter. */
export function validateReason(text: string): AppointmentFieldError | undefined {
  return text.trim().length > APPOINTMENT_REASON_MAX ? "reasonTooLong" : undefined;
}

/** Anın Türkiye günü ("yyyy-MM-dd"). */
export function turkeyDayOf(at: string): string {
  return new Date(Date.parse(at) + TURKEY_OFFSET_MS).toISOString().slice(0, 10);
}

/** Anın Türkiye saati ("SS:DD"). */
export function turkeyTimeOf(at: string): string {
  return new Date(Date.parse(at) + TURKEY_OFFSET_MS).toISOString().slice(11, 16);
}

/** Günün haftanın günü adı (`Monday`…`Sunday`; ekran metni `day.<ad>`). */
export function weekdayOf(day: string): string {
  return WEEK[new Date(`${day}T00:00:00Z`).getUTCDay()];
}

/** Gün ve ay numarası: "2026-11-26" → { day: 26, month: 11 } (ekran "26 Kasım"). */
export function dayAndMonth(day: string): { day: number; month: number } {
  return { day: Number(day.slice(8, 10)), month: Number(day.slice(5, 7)) };
}

/** Kayıtları Türkiye gününe göre gruplar; sıra korunur (sunucu saat sırasıyla verir). */
export function groupByTurkeyDay<T>(items: readonly T[], startsAt: (item: T) => string): DayGroup<T>[] {
  const groups: DayGroup<T>[] = [];
  for (const item of items) {
    const day = turkeyDayOf(startsAt(item));
    const last = groups[groups.length - 1];
    if (last && last.day === day) {
      last.items.push(item);
    } else {
      groups.push({ day, items: [item] });
    }
  }

  return groups;
}
