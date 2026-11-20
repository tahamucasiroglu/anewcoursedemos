// Veli iletişimi formlarının alan kuralları ve ortak gösterim mantığı (spec 0011): sunucudaki kurallarla aynı; hatalı alan
// varsa istek gönderilmez. Sonuç metin değil anahtardır; ekran metni kendi tr.json'undan alır. Tarihler "yyyy-MM-dd",
// saatler "SS:DD" (Türkiye saati). Web ve mobil aynı kuralları buradan kullanır.
import type { components } from "../schema";

export type StaffAnnouncementRow = components["schemas"]["StaffAnnouncementRow"];
export type AnnouncementPublished = components["schemas"]["AnnouncementPublished"];
export type AnnouncementDetail = components["schemas"]["AnnouncementDetail"];
export type AnnouncementResults = components["schemas"]["AnnouncementResults"];
export type ConsentChildView = components["schemas"]["ConsentChildView"];
export type ConsentSummary = components["schemas"]["ConsentSummary"];
export type UnreadGuardianRow = components["schemas"]["UnreadGuardianRow"];
export type FeedPage = components["schemas"]["FeedPage"];
export type FeedItem = components["schemas"]["FeedItem"];
export type ConversationRow = components["schemas"]["ConversationRow"];
export type ConversationThread = components["schemas"]["ConversationThread"];
export type MessageView = components["schemas"]["MessageView"];
export type MessageSent = components["schemas"]["MessageSent"];
export type MessageContacts = components["schemas"]["MessageContacts"];
export type ChildTeachers = components["schemas"]["ChildTeachers"];
export type ClassroomContacts = components["schemas"]["ClassroomContacts"];
export type StudentContacts = components["schemas"]["StudentContacts"];
export type ContactPerson = components["schemas"]["ContactPerson"];
export type QuietHoursView = components["schemas"]["QuietHoursView"];

export const ANNOUNCEMENT_TITLE_MAX = 150;
export const ANNOUNCEMENT_BODY_MAX = 4000;
export const CONSENT_QUESTION_MAX = 300;
export const CONSENT_MAX_DAYS = 180;
export const MESSAGE_MAX = 2000;
export const QUIET_STEP_MINUTES = 15;

export type AnnouncementScope = "School" | "Branch" | "Classroom" | "Student";
export const ANNOUNCEMENT_SCOPES: readonly AnnouncementScope[] = ["School", "Branch", "Classroom", "Student"];
/** Öğretmenin seçebileceği kapsamlar (okul ve şube yalnızca kurucuda). */
export const TEACHER_SCOPES: readonly AnnouncementScope[] = ["Classroom", "Student"];

export type WeekDay = "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday" | "Sunday";
export const WEEK_DAYS: readonly WeekDay[] = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

export type CommunicationFieldError =
  | "titleRequired"
  | "titleTooLong"
  | "bodyRequired"
  | "bodyTooLong"
  | "scopeRequired"
  | "questionRequired"
  | "questionTooLong"
  | "deadlineRequired"
  | "deadlineRange"
  | "messageRequired"
  | "messageTooLong"
  | "startInvalid"
  | "endInvalid"
  | "startBeforeEnd"
  | "daysRequired";

export interface AnnouncementFormValues {
  title: string;
  body: string;
  scope: AnnouncementScope;
  scopeId: string;
  requestConsent: boolean;
  consentQuestion: string;
  consentDeadline: string;
}

export type AnnouncementFormErrors = Partial<Record<keyof AnnouncementFormValues, CommunicationFieldError>>;

const DAY_PATTERN = /^\d{4}-\d{2}-\d{2}$/;
const TIME_PATTERN = /^([01]\d|2[0-3]):([0-5]\d)$/;
const TURKEY_OFFSET_MS = 3 * 3_600_000;

/** Türkiye günü ("yyyy-MM-dd"): Türkiye 2016'dan beri yaz saati uygulamıyor, sabit UTC+3 (sunucunun yedeğiyle aynı). */
export function turkeyToday(now: Date = new Date()): string {
  return new Date(now.getTime() + TURKEY_OFFSET_MS).toISOString().slice(0, 10);
}

function addDays(day: string, days: number): string {
  return new Date(Date.parse(`${day}T00:00:00Z`) + days * 86_400_000).toISOString().slice(0, 10);
}

/** Onay formunun son günü yarından (dahil) en geç 180 gün sonrasına (dahil) mı. */
export function isDeadlineInRange(deadline: string, today: string): boolean {
  return DAY_PATTERN.test(deadline) && deadline > today && deadline <= addDays(today, CONSENT_MAX_DAYS);
}

/** Duyuru formu: başlık/metin zorunlu ve sınırlı, okul dışında kapsam seçilmeli, "Onay iste"de soru ve son gün. */
export function validateAnnouncementForm(values: AnnouncementFormValues, today: string): AnnouncementFormErrors {
  const errors: AnnouncementFormErrors = {};
  const title = values.title.trim();
  const body = values.body.trim();
  if (title === "") {
    errors.title = "titleRequired";
  } else if (title.length > ANNOUNCEMENT_TITLE_MAX) {
    errors.title = "titleTooLong";
  }

  if (body === "") {
    errors.body = "bodyRequired";
  } else if (body.length > ANNOUNCEMENT_BODY_MAX) {
    errors.body = "bodyTooLong";
  }

  if (values.scope !== "School" && values.scopeId === "") {
    errors.scopeId = "scopeRequired";
  }

  if (values.requestConsent) {
    const question = values.consentQuestion.trim();
    if (question === "") {
      errors.consentQuestion = "questionRequired";
    } else if (question.length > CONSENT_QUESTION_MAX) {
      errors.consentQuestion = "questionTooLong";
    }

    if (values.consentDeadline === "") {
      errors.consentDeadline = "deadlineRequired";
    } else if (!isDeadlineInRange(values.consentDeadline, today)) {
      errors.consentDeadline = "deadlineRange";
    }
  }

  return errors;
}

/** Yayın isteğinin gövdesi (kırpılmış alanlar; onay istenmediyse soru ve son gün gönderilmez). */
export function announcementBody(values: AnnouncementFormValues) {
  return {
    title: values.title.trim(),
    body: values.body.trim(),
    scope: values.scope,
    scopeId: values.scope === "School" ? null : values.scopeId,
    requestConsent: values.requestConsent,
    consentQuestion: values.requestConsent ? values.consentQuestion.trim() : null,
    consentDeadline: values.requestConsent ? values.consentDeadline : null,
  };
}

/** Mesaj metni: boş olamaz, kırpılmış en fazla 2000 karakter. */
export function validateMessage(text: string): CommunicationFieldError | undefined {
  const body = text.trim();
  if (body === "") {
    return "messageRequired";
  }

  return body.length > MESSAGE_MAX ? "messageTooLong" : undefined;
}

export interface QuietHoursFormValues {
  start: string;
  end: string;
  days: readonly WeekDay[];
}

export type QuietHoursFormErrors = Partial<Record<keyof QuietHoursFormValues, CommunicationFieldError>>;

/** "SS:DD" biçiminde ve 15 dakikalık adımda mı. */
export function isQuarterHour(value: string): boolean {
  const match = TIME_PATTERN.exec(value);
  return match !== null && Number(match[2]) % QUIET_STEP_MINUTES === 0;
}

/** Mesai saatleri: iki saat de 15 dk adımında, başlangıç bitişten önce, en az bir gün. */
export function validateQuietHours(values: QuietHoursFormValues): QuietHoursFormErrors {
  const errors: QuietHoursFormErrors = {};
  if (!isQuarterHour(values.start)) {
    errors.start = "startInvalid";
  }

  if (!isQuarterHour(values.end)) {
    errors.end = "endInvalid";
  }

  if (!errors.start && !errors.end && values.start >= values.end) {
    errors.start = "startBeforeEnd";
  }

  if (values.days.length === 0) {
    errors.days = "daysRequired";
  }

  return errors;
}

/** Mesai saatleri isteğinin gövdesi; günler hafta sırasıyla. */
export function quietHoursBody(values: QuietHoursFormValues) {
  return { enabled: true, start: values.start, end: values.end, days: WEEK_DAYS.filter((d) => values.days.includes(d)) };
}

/** Gün seçimini açar/kapatır (hafta sırası korunur). */
export function toggleDay(days: readonly WeekDay[], day: WeekDay): WeekDay[] {
  return days.includes(day) ? days.filter((d) => d !== day) : WEEK_DAYS.filter((d) => d === day || days.includes(d));
}

/** Onay sonuç sayıları ("15 onay, 2 ret, 5 cevapsız" için). */
export function consentCounts(summary: ConsentSummary): { approved: number; declined: number; pending: number } {
  return { approved: Number(summary.approved), declined: Number(summary.declined), pending: Number(summary.pending) };
}

/** "Mesajlar" ya da "Mesajlar (3)" gibi sayılı bağlantı yazısı. */
export function countedText(count: number | string | undefined, plain: string, withCount: string): string {
  const n = Number(count ?? 0);
  return n > 0 ? withCount.replace("{n}", String(n)) : plain;
}
