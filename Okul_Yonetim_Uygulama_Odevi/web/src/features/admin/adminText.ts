import type { AcademicFieldError, AdminFieldError, RoleName } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

// Alan hatası ve rol adlarının Türkçe metinleri (tr.json).
export function fieldText(error: AdminFieldError): string {
  return t(`field.${error}` as TextKey);
}

export function roleText(role: string): string {
  return ["Founder", "Teacher", "Guardian", "Accounting"].includes(role) ? t(`role.${role as RoleName}` as TextKey) : role;
}

export function failureText(outcome: { unreachable: true } | { unreachable: false; message: string | null }): string {
  return outcome.unreachable ? t("admin.unreachable") : outcome.message ?? t("admin.loadError");
}

// Akademik yapı formlarının alan hataları (spec 0008): ad hataları formun kendi metnini (seviye/dönem/sınıf), diğerleri
// ortak metni kullanır.
export function academicFieldText(error: AcademicFieldError, scope: "levels" | "terms" | "classrooms" | "calendarAdmin"): string {
  if ((error === "nameRequired" || error === "nameTooLong") && scope !== "calendarAdmin") {
    return t(`${scope}.field.${error}` as TextKey);
  }

  return t(`academic.field.${error}` as TextKey);
}
