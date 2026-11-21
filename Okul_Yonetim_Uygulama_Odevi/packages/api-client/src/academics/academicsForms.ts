// Ödev ve not ekranlarının ortak mantığı (spec 0013): alan sınırları (sunucuyla aynı), "GG.AA.YYYY" gün alanı, teslim günü
// aralığı, puan ayrıştırma, teslim işaretleri ve sayıları, düğme ve sonuç metinleri. Metin şablonları ekranın tr.json'undan
// gelir ({n} gibi yer tutucularla); burada yalnızca hangi parçanın hangi sayıyla yazılacağı belirlenir.
import type { components } from "../schema";
import { fillText } from "../attendance/attendanceForms";

export type AssignmentRow = components["schemas"]["AssignmentRow"];
export type AssignmentCreated = components["schemas"]["AssignmentCreated"];
export type AssignmentSubmissionsView = components["schemas"]["AssignmentSubmissionsView"];
export type SubmissionStudentRow = components["schemas"]["SubmissionStudentRow"];
export type SubmissionsSaveResult = components["schemas"]["SubmissionsSaveResult"];
export type AssessmentRow = components["schemas"]["AssessmentRow"];
export type AssessmentCreated = components["schemas"]["AssessmentCreated"];
export type AssessmentGradesView = components["schemas"]["AssessmentGradesView"];
export type GradeStudentRow = components["schemas"]["GradeStudentRow"];
export type GradesSaveResult = components["schemas"]["GradesSaveResult"];
export type ChildAcademicsView = components["schemas"]["ChildAcademicsView"];
export type StudentAssignmentRecord = components["schemas"]["StudentAssignmentRecord"];
export type StudentGradeRecord = components["schemas"]["StudentGradeRecord"];

export const SUBJECT_MAX = 60;
export const ASSIGNMENT_TITLE_MAX = 150;
export const DESCRIPTION_MAX = 2000;
export const ASSESSMENT_TITLE_MAX = 100;
export const DUE_MAX_DAYS = 90;

export type SubmissionStatus = "Submitted" | "NotSubmitted" | "Unmarked";
export const SUBMISSION_STATUSES: readonly SubmissionStatus[] = ["Submitted", "NotSubmitted", "Unmarked"];
export type SubmissionMarks = Record<string, SubmissionStatus>;
export interface SubmissionCounts { submitted: number; notSubmitted: number; unmarked: number }

export type AssessmentKind = "Written" | "Oral";
export const ASSESSMENT_KINDS: readonly AssessmentKind[] = ["Written", "Oral"];

export type AcademicsFieldError = "required" | "tooLong" | "invalidDate" | "dueOutOfRange" | "future";

export interface AssignmentFormValues { subject: string; title: string; description: string; dueDate: string }
export interface AssignmentFormErrors { subject?: AcademicsFieldError; title?: AcademicsFieldError; description?: AcademicsFieldError; dueDate?: AcademicsFieldError }
export interface AssessmentFormValues { subject: string; kind: AssessmentKind; title: string; date: string }
export interface AssessmentFormErrors { subject?: AcademicsFieldError; title?: AcademicsFieldError; date?: AcademicsFieldError }

/** "2026-11-20" + n gün (takvim günü; saat dilimi etkisiz). */
export function addDays(day: string, days: number): string {
  const ms = Date.parse(`${day}T00:00:00Z`);
  return Number.isNaN(ms) ? day : new Date(ms + days * 86_400_000).toISOString().slice(0, 10);
}

/** "25.11.2026" → "2026-11-25"; geçersiz ya da olmayan gün (31.02) → null. */
export function parseDayInput(text: string): string | null {
  const match = /^(\d{1,2})\.(\d{1,2})\.(\d{4})$/.exec(text.trim());
  if (!match) {
    return null;
  }

  const iso = `${match[3]}-${match[2].padStart(2, "0")}-${match[1].padStart(2, "0")}`;
  const ms = Date.parse(`${iso}T00:00:00Z`);
  return !Number.isNaN(ms) && new Date(ms).toISOString().slice(0, 10) === iso ? iso : null;
}

/** "2026-11-25" → "25.11.2026" (gün alanının başlangıç değeri). */
export function toDayInput(day: string): string {
  const [year, month, date] = day.split("-");
  return year && month && date ? `${date}.${month}.${year}` : day;
}

function textError(value: string, max: number): AcademicsFieldError | undefined {
  const clean = value.trim();
  return clean === "" ? "required" : clean.length > max ? "tooLong" : undefined;
}

/** Ödev formu (sunucuyla aynı kurallar): ders ≤ 60, başlık ≤ 150 zorunlu, açıklama ≤ 2000, teslim günü bugün … bugün + 90. */
export function validateAssignmentForm(values: AssignmentFormValues, today: string): AssignmentFormErrors {
  const errors: AssignmentFormErrors = {};
  const subject = textError(values.subject, SUBJECT_MAX);
  const title = textError(values.title, ASSIGNMENT_TITLE_MAX);
  if (subject) {
    errors.subject = subject;
  }

  if (title) {
    errors.title = title;
  }

  if (values.description.trim().length > DESCRIPTION_MAX) {
    errors.description = "tooLong";
  }

  const due = parseDayInput(values.dueDate);
  if (values.dueDate.trim() === "") {
    errors.dueDate = "required";
  } else if (!due) {
    errors.dueDate = "invalidDate";
  } else if (due < today || due > addDays(today, DUE_MAX_DAYS)) {
    errors.dueDate = "dueOutOfRange";
  }

  return errors;
}

export function assignmentBody(values: AssignmentFormValues) {
  const description = values.description.trim();
  return { subject: values.subject.trim(), title: values.title.trim(), description: description === "" ? null : description, dueDate: parseDayInput(values.dueDate) ?? values.dueDate };
}

/** Değerlendirme formu: ders ≤ 60, başlık ≤ 100 zorunlu, gün bugünden sonra olamaz (dönem denetimi sunucuda). */
export function validateAssessmentForm(values: AssessmentFormValues, today: string): AssessmentFormErrors {
  const errors: AssessmentFormErrors = {};
  const subject = textError(values.subject, SUBJECT_MAX);
  const title = textError(values.title, ASSESSMENT_TITLE_MAX);
  if (subject) {
    errors.subject = subject;
  }

  if (title) {
    errors.title = title;
  }

  const date = parseDayInput(values.date);
  if (values.date.trim() === "") {
    errors.date = "required";
  } else if (!date) {
    errors.date = "invalidDate";
  } else if (date > today) {
    errors.date = "future";
  }

  return errors;
}

export function assessmentBody(values: AssessmentFormValues) {
  return { subject: values.subject.trim(), kind: values.kind, title: values.title.trim(), date: parseDayInput(values.date) ?? values.date };
}

/** Puan alanı: boş = girilmedi (null); yalnızca rakam, 0–100 (ondalık, işaret ve harf geçersiz). */
export function parseScore(text: string): { ok: true; value: number | null } | { ok: false } {
  const clean = text.trim();
  if (clean === "") {
    return { ok: true, value: null };
  }

  if (!/^\d{1,3}$/.test(clean)) {
    return { ok: false };
  }

  const value = Number(clean);
  return value <= 100 ? { ok: true, value } : { ok: false };
}

/** Geçersiz puanı olan öğrencilerin kimlikleri (liste sırasıyla). */
export function invalidScores(scores: Record<string, string>): string[] {
  return Object.entries(scores).filter(([, text]) => !parseScore(text).ok).map(([id]) => id);
}

export function initialScores(students: readonly GradeStudentRow[]): Record<string, string> {
  return Object.fromEntries(students.map((s) => [s.studentId, s.score === null || s.score === undefined ? "" : String(s.score)]));
}

/**
 * Puan kaydının gövdesi: yalnızca ilk yüklemeden (ya da son başarılı kayıttan) beri değişen öğrenciler gider — sunucu
 * gönderilmeyen öğrenciye dokunmaz (plan A-5), eski ekrandan kayıt başkasının girdiği puanı silmez (review round 1, R1-3).
 * Hiçbir şey değişmediyse <c>null</c> (istek gönderilmez). Geçersiz puan gönderilmeden önce ekranda yakalanır.
 */
export function gradesBody(scores: Record<string, string>, initial: Record<string, string>) {
  const grades = Object.entries(scores)
    .filter(([studentId, text]) => text.trim() !== (initial[studentId] ?? "").trim())
    .map(([studentId, text]) => {
      const parsed = parseScore(text);
      return { studentId, score: parsed.ok ? parsed.value : text };
    });
  return grades.length > 0 ? { grades } : null;
}

/**
 * Sunucunun puan alanı hatalarındaki öğrenci kimlikleri (`Grades[<öğrenci>].Score`, plan Risks 6) — ekran o öğrencinin alanını
 * işaretler.
 */
export function scoreErrorStudents(fields: readonly { property: string }[] | undefined): string[] {
  const ids: string[] = [];
  for (const { property } of fields ?? []) {
    const match = /^grades\[([^\]]+)\]\.score$/i.exec(property);
    if (match && !ids.includes(match[1])) {
      ids.push(match[1]);
    }
  }

  return ids;
}

function asStatus(value: string): SubmissionStatus {
  return value === "Submitted" || value === "NotSubmitted" ? value : "Unmarked";
}

export function initialSubmissionMarks(students: readonly SubmissionStudentRow[]): SubmissionMarks {
  return Object.fromEntries(students.map((s) => [s.studentId, asStatus(s.status)]));
}

export function countSubmissions(marks: SubmissionMarks): SubmissionCounts {
  const counts: SubmissionCounts = { submitted: 0, notSubmitted: 0, unmarked: 0 };
  for (const status of Object.values(marks)) {
    if (status === "Submitted") {
      counts.submitted++;
    } else if (status === "NotSubmitted") {
      counts.notSubmitted++;
    } else {
      counts.unmarked++;
    }
  }

  return counts;
}

/**
 * Teslim kaydının gövdesi: yalnızca ilk yüklemeden (ya da son başarılı kayıttan) beri durumu değişen öğrenciler (R1-3; sunucu
 * gönderilmeyene dokunmaz). Hiçbir şey değişmediyse <c>null</c> (istek gönderilmez).
 */
export function submissionsBody(marks: SubmissionMarks, initial: SubmissionMarks) {
  const entries = Object.entries(marks)
    .filter(([studentId, status]) => status !== initial[studentId])
    .map(([studentId, status]) => ({ studentId, status }));
  return entries.length > 0 ? { entries } : null;
}

/** "Kaydet" ya da "Kaydet (3 etmedi)". */
export function submissionsButtonText(counts: SubmissionCounts, texts: { plain: string; withNotSubmitted: string }): string {
  return counts.notSubmitted > 0 ? fillText(texts.withNotSubmitted, { n: counts.notSubmitted }) : texts.plain;
}

/** "Kaydedildi — 27 teslim etti, 3 etmedi." */
export function submissionsResultText(result: SubmissionsSaveResult, template: string): string {
  return fillText(template, { submitted: Number(result.submitted), notSubmitted: Number(result.notSubmitted) });
}

/** "Kaydedildi — 28 / 30 puan girildi." */
export function gradesResultText(result: GradesSaveResult, template: string): string {
  return fillText(template, { graded: Number(result.gradedCount), total: Number(result.studentCount) });
}
