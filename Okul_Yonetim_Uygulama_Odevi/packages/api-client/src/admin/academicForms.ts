// Akademik yapı formlarının alan kuralları (spec 0008): sunucudaki kurallarla aynı; hatalı alan varsa istek gönderilmez.
// Sonuç metin değil anahtardır; ekran metni kendi tr.json'undan alır. Tarihler "yyyy-MM-dd" (tarih alanının değeri).
import type { components } from "../schema";

export type GradeLevelAdminView = components["schemas"]["GradeLevelAdminView"];
export type TermAdminView = components["schemas"]["TermAdminView"];
export type ClassroomAdminView = components["schemas"]["ClassroomAdminView"];
export type ClassroomAdminList = components["schemas"]["ClassroomAdminList"];
export type CalendarEntryView = components["schemas"]["CalendarEntryView"];

export type CalendarKind = "Holiday" | "Exam" | "Event";
export const CALENDAR_KINDS: readonly CalendarKind[] = ["Holiday", "Exam", "Event"];

export const GRADE_LEVEL_NAME_MAX = 50;
export const TERM_NAME_MAX = 100;
export const CLASSROOM_NAME_MAX = 30;
export const CALENDAR_TITLE_MAX = 100;
export const TERM_MAX_DAYS = 366;

export type AcademicFieldError =
  | "nameRequired"
  | "nameTooLong"
  | "sortOrderRange"
  | "datesRequired"
  | "termEndBeforeStart"
  | "termTooLong"
  | "entryEndBeforeStart"
  | "entryOutsideTerm"
  | "titleRequired"
  | "titleTooLong"
  | "kindRequired"
  | "branchRequired"
  | "levelRequired";

const DAY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

function isDay(value: string): boolean {
  return DAY_PATTERN.test(value) && !Number.isNaN(Date.parse(`${value}T00:00:00Z`));
}

/** Uç günler dahil gün sayısı (sunucudaki Term.LengthInDays ile aynı): 2026-09-01 – 2027-09-01 → 366. */
export function lengthInDays(startDate: string, endDate: string): number {
  return Math.round((Date.parse(`${endDate}T00:00:00Z`) - Date.parse(`${startDate}T00:00:00Z`)) / 86_400_000) + 1;
}

function nameError(value: string, max: number): AcademicFieldError | undefined {
  const name = value.trim();
  if (name === "") {
    return "nameRequired";
  }

  return name.length > max ? "nameTooLong" : undefined;
}

export interface GradeLevelFormValues {
  name: string;
  sortOrder: string;
}

export type GradeLevelFormErrors = Partial<Record<keyof GradeLevelFormValues, AcademicFieldError>>;

export function validateGradeLevelForm(values: GradeLevelFormValues): GradeLevelFormErrors {
  const errors: GradeLevelFormErrors = {};
  const name = nameError(values.name, GRADE_LEVEL_NAME_MAX);
  if (name) {
    errors.name = name;
  }

  const order = values.sortOrder.trim();
  if (!/^\d+$/.test(order) || Number(order) < 1 || Number(order) > 99) {
    errors.sortOrder = "sortOrderRange";
  }

  return errors;
}

/** Yeni seviye formunun varsayılan sırası: mevcut en büyük sıra + 1 (en fazla 99). */
export function nextSortOrder(levels: readonly { sortOrder: number | string }[]): number {
  const max = levels.reduce((highest, level) => Math.max(highest, Number(level.sortOrder)), 0);
  return Math.min(max + 1, 99);
}

export interface TermFormValues {
  name: string;
  startDate: string;
  endDate: string;
}

export type TermFormErrors = Partial<Record<keyof TermFormValues, AcademicFieldError>>;

export function validateTermForm(values: TermFormValues): TermFormErrors {
  const errors: TermFormErrors = {};
  const name = nameError(values.name, TERM_NAME_MAX);
  if (name) {
    errors.name = name;
  }

  if (!isDay(values.startDate)) {
    errors.startDate = "datesRequired";
  }

  if (!isDay(values.endDate)) {
    errors.endDate = "datesRequired";
  } else if (!errors.startDate) {
    if (values.endDate <= values.startDate) {
      errors.endDate = "termEndBeforeStart";
    } else if (lengthInDays(values.startDate, values.endDate) > TERM_MAX_DAYS) {
      errors.endDate = "termTooLong";
    }
  }

  return errors;
}

export interface CalendarEntryFormValues {
  title: string;
  kind: string;
  startDate: string;
  endDate: string;
}

export type CalendarEntryFormErrors = Partial<Record<keyof CalendarEntryFormValues, AcademicFieldError>>;

/** Kaydın iki günü de dönemin içinde olmalı (uç günler dahil). */
export function validateCalendarEntryForm(
  values: CalendarEntryFormValues,
  term: { startDate: string; endDate: string },
): CalendarEntryFormErrors {
  const errors: CalendarEntryFormErrors = {};
  const title = values.title.trim();
  if (title === "") {
    errors.title = "titleRequired";
  } else if (title.length > CALENDAR_TITLE_MAX) {
    errors.title = "titleTooLong";
  }

  if (!CALENDAR_KINDS.includes(values.kind as CalendarKind)) {
    errors.kind = "kindRequired";
  }

  if (!isDay(values.startDate)) {
    errors.startDate = "datesRequired";
  } else if (values.startDate < term.startDate || values.startDate > term.endDate) {
    errors.startDate = "entryOutsideTerm";
  }

  if (!isDay(values.endDate)) {
    errors.endDate = "datesRequired";
  } else if (isDay(values.startDate) && values.endDate < values.startDate) {
    errors.endDate = "entryEndBeforeStart";
  } else if (values.endDate < term.startDate || values.endDate > term.endDate) {
    errors.endDate = "entryOutsideTerm";
  }

  return errors;
}

export interface ClassroomFormValues {
  branchId: string;
  gradeLevelId: string;
  name: string;
}

export type ClassroomFormErrors = Partial<Record<keyof ClassroomFormValues, AcademicFieldError>>;

/** <c>requireBranch</c>: açarken şube zorunlu; düzenlemede şube değişmez, denetlenmez. */
export function validateClassroomForm(values: ClassroomFormValues, requireBranch = true): ClassroomFormErrors {
  const errors: ClassroomFormErrors = {};
  const name = nameError(values.name, CLASSROOM_NAME_MAX);
  if (name) {
    errors.name = name;
  }

  if (requireBranch && values.branchId === "") {
    errors.branchId = "branchRequired";
  }

  if (values.gradeLevelId === "") {
    errors.gradeLevelId = "levelRequired";
  }

  return errors;
}
