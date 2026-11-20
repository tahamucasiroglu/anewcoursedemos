// İletişim ekranlarının Türkçe metin yardımcıları (spec 0011): kapsam, alan hatası ve onay cevabı metinleri tr.json'dan;
// "{n}" gibi yer tutucular tek geçişte doldurulur.
import { fillText, type CommunicationFieldError } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

export const fill = fillText;

export function scopeText(scope: string): string {
  return ["School", "Branch", "Classroom", "Student"].includes(scope) ? t(`communication.scope.${scope}` as TextKey) : scope;
}

export function fieldText(error: CommunicationFieldError): string {
  return t(`communication.field.${error}` as TextKey);
}

export function answerText(answer: string | null): string {
  return answer === "Approved" || answer === "Declined" ? t(`communication.answer.${answer}` as TextKey) : t("communication.answer.none");
}
