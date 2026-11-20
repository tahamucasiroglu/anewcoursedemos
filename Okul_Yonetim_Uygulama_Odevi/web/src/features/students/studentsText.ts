import type { StudentFieldError } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

// Öğrenci formlarının alan hatası ve yakınlık metinleri (tr.json, spec 0009).
export function studentFieldText(error: StudentFieldError): string {
  return t(`students.field.${error}` as TextKey);
}

export function relationshipText(relationship: string): string {
  return ["Mother", "Father", "Other"].includes(relationship) ? t(`students.relationship.${relationship}` as TextKey) : relationship;
}
