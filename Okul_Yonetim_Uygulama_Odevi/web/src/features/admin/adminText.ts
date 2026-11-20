import type { AdminFieldError, RoleName } from "@okul/api-client";
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
