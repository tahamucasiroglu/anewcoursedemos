import { fillText, type ReportFieldError } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

// Yönetici paneli metin yardımcıları (spec 0017): yer tutuculu metinler ve süzgeç alan hataları tr.json'dan; sayılar metinle yazılır
// ("12 öğrenci yok") — renk tek başına anlam taşımaz.
export function fill(key: TextKey, values: Record<string, string | number>): string {
  return fillText(t(key), values);
}

export function reportFieldText(error: ReportFieldError, field: "startDate" | "endDate" | "minAbsent"): string {
  if (error === "required") {
    return t(field === "startDate" ? "dashboard.field.startRequired" : "dashboard.field.endRequired");
  }

  return t(`dashboard.field.${error}`);
}
