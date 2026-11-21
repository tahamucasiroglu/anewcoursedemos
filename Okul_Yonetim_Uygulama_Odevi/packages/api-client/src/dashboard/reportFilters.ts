// Yönetici paneli (spec 0017) istemci mantığı: rapor süzgecinin ön doğrulaması (sınırlar sunucuyla aynı — tarih zorunlu, başlangıç ≤ bitiş,
// uçlar dahil en fazla 366 gün, eşik 0–366; sunucu yine ayrıca doğrular), varsayılan aralık (bu ayın ilk günü – bugün, Türkiye günü),
// sorgu parametreleri ve dışa aktarılan dosyanın adı (Content-Disposition). Metinler ekranın tr.json'undan gelir.
import { isIsoDay } from "../billing/billingForms";
import type { components } from "../schema";

export type AttendanceCard = components["schemas"]["AttendanceCard"];
export type DayClassroomRow = components["schemas"]["DayClassroomRow"];
export type AbsentStudentRow = components["schemas"]["AbsentStudentRow"];
export type OverdueCard = components["schemas"]["OverdueCard"];
export type OverdueStudent = components["schemas"]["OverdueStudent"];
export type UnreadAnnouncementsCard = components["schemas"]["UnreadAnnouncementsCard"];
export type UnreadAnnouncementRow = components["schemas"]["UnreadAnnouncementRow"];
export type AttendanceReportPage = components["schemas"]["AttendanceReportPage"];
export type AttendanceReportRow = components["schemas"]["AttendanceReportRow"];
export type PaymentsReportPage = components["schemas"]["PaymentsReportPage"];
export type PaymentReportRow = components["schemas"]["PaymentReportRow"];
export type PaymentMethodTotal = components["schemas"]["PaymentMethodTotal"];
export type BalancesReportPage = components["schemas"]["BalancesReportPage"];
export type BalanceReportRow = components["schemas"]["BalanceReportRow"];

export const REPORT_PAGE_SIZE = 50;
export const REPORT_MAX_DAYS = 366;
export const MIN_ABSENT_MAX = 366;
export const SUMMARY_TOP = 10;

export type ReportKind = "attendance" | "payments" | "balances";
export const REPORT_KINDS: readonly ReportKind[] = ["attendance", "payments", "balances"];

export interface ReportFilterValues {
  startDate: string;
  endDate: string;
  branchId: string;
  classroomId: string;
  minAbsent: string;
  overdueOnly: boolean;
}

export type ReportFieldError = "required" | "inverted" | "tooLong" | "minAbsentInvalid";
export interface ReportFilterErrors { startDate?: ReportFieldError; endDate?: ReportFieldError; minAbsent?: ReportFieldError }

/** Varsayılan süzgeç: bu ayın ilk günü – bugün (gün metinleri "yyyy-aa-gg", bugün Türkiye günü), bütün şubeler. */
export function defaultReportFilter(today: string): ReportFilterValues {
  return { startDate: `${today.slice(0, 8)}01`, endDate: today, branchId: "", classroomId: "", minAbsent: "", overdueOnly: false };
}

/** Uçlar dahil gün sayısı (1 Kasım – 1 Kasım = 1). */
export function rangeDays(startDate: string, endDate: string): number {
  return Math.round((Date.parse(`${endDate}T00:00:00Z`) - Date.parse(`${startDate}T00:00:00Z`)) / 86_400_000) + 1;
}

/** Süzgeç alan hataları; borç raporunda tarih yoktur, eşik yalnızca devamsızlık raporunda. Hata varsa istek gönderilmez. */
export function validateReportFilter(kind: ReportKind, values: ReportFilterValues): ReportFilterErrors {
  const errors: ReportFilterErrors = {};
  if (kind !== "balances") {
    if (!isIsoDay(values.startDate)) {
      errors.startDate = "required";
    }

    if (!isIsoDay(values.endDate)) {
      errors.endDate = "required";
    }

    if (!errors.startDate && !errors.endDate) {
      if (values.startDate > values.endDate) {
        errors.endDate = "inverted";
      } else if (rangeDays(values.startDate, values.endDate) > REPORT_MAX_DAYS) {
        errors.endDate = "tooLong";
      }
    }
  }

  if (kind === "attendance" && values.minAbsent.trim() !== "") {
    const raw = values.minAbsent.trim();
    if (!/^\d{1,3}$/.test(raw) || Number(raw) > MIN_ABSENT_MAX) {
      errors.minAbsent = "minAbsentInvalid";
    }
  }

  return errors;
}

/** Rapora (ve aynı süzgeçle dışa aktarmaya) giden sorgu parametreleri; boş alanlar gönderilmez. */
/** Rapor ve dışa aktarma uçlarının ortak sorgu biçimi (her uç yalnızca kendi alanlarını okur). */
export interface ReportQuery {
  startDate?: string;
  endDate?: string;
  branchId?: string;
  classroomId?: string;
  minAbsent?: number;
  overdueOnly?: boolean;
  page?: number;
}

export function reportQuery(kind: ReportKind, values: ReportFilterValues): ReportQuery {
  const query: ReportQuery = {};
  if (kind !== "balances") {
    query.startDate = values.startDate;
    query.endDate = values.endDate;
  }

  if (values.branchId) {
    query.branchId = values.branchId;
  }

  if (kind === "attendance") {
    if (values.classroomId) {
      query.classroomId = values.classroomId;
    }

    if (values.minAbsent.trim() !== "") {
      query.minAbsent = Number(values.minAbsent.trim());
    }
  }

  if (kind === "balances") {
    query.overdueOnly = values.overdueOnly;
  }

  return query;
}

/** Content-Disposition başlığındaki dosya adı (filename*=UTF-8'' ya da filename=); yoksa verilen yedek ad. */
export function exportFileName(contentDisposition: string | null, fallback: string): string {
  if (!contentDisposition) {
    return fallback;
  }

  const star = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
  if (star) {
    try {
      return decodeURIComponent(star[1].trim());
    } catch {
      return fallback;
    }
  }

  const plain = /filename="?([^";]+)"?/i.exec(contentDisposition);
  return plain ? plain[1].trim() : fallback;
}

