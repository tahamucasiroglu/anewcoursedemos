// Haftalık gelişim özeti ekranının ortak kuralları (spec 0014): öğretmen notu en fazla 500, onay metni boş olamaz ve en fazla 1500
// karakter (sunucuyla aynı — kırpılmış hâliyle sayılır; AC-8: hatalı alanda istek gitmez), hafta seçimi ("Bu hafta" / "Geçen
// hafta") ve durum anahtarları ("Özet yok" / "Taslak hazır" / "Gönderildi"). Metinler ekranın tr.json'undan gelir.
import type { components } from "../schema";

export type WeeklySummaryClassroom = components["schemas"]["WeeklySummaryClassroom"];
export type WeeklySummaryList = components["schemas"]["WeeklySummaryList"];
export type WeeklySummaryStudentRow = components["schemas"]["WeeklySummaryStudentRow"];
export type WeeklySummaryPanel = components["schemas"]["WeeklySummaryPanel"];
export type WeeklySummarySource = components["schemas"]["WeeklySummarySource"];
export type WeeklySummarySent = components["schemas"]["WeeklySummarySent"];

export const TEACHER_NOTE_MAX = 500;
export const SUMMARY_TEXT_MAX = 1500;

export type WeekChoice = "current" | "previous";
export const WEEK_CHOICES: readonly WeekChoice[] = ["current", "previous"];

export type SummaryStatusKey = "none" | "draft" | "sent";
export type SummaryFieldError = "required" | "tooLong";

/** Sunucunun durumu (`None` / `Draft` / `Sent`) → ekran anahtarı. */
export function summaryStatusKey(status: string): SummaryStatusKey {
  return status === "Draft" ? "draft" : status === "Sent" ? "sent" : "none";
}

/** Öğretmen notu isteğe bağlı; kırpılmış hâliyle 500'ü aşarsa hata. */
export function validateTeacherNote(note: string): SummaryFieldError | null {
  return note.trim().length > TEACHER_NOTE_MAX ? "tooLong" : null;
}

/** Onaylanacak özet metni: boş olamaz, kırpılmış hâliyle en fazla 1500. */
export function validateSummaryText(text: string): SummaryFieldError | null {
  const trimmed = text.trim();
  if (trimmed === "") {
    return "required";
  }

  return trimmed.length > SUMMARY_TEXT_MAX ? "tooLong" : null;
}

/** Dayanak boşken "Özet oluştur" yalnızca öğretmen notu yazılınca açılır (spec 0014 UX). */
export function canGenerate(hasData: boolean, note: string): boolean {
  return hasData || note.trim() !== "";
}
