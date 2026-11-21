import { fillText, isInstallmentState, isPaymentMethod, type BillingFieldError } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";

// Tahsilat ekranlarının metin yardımcıları (spec 0015): alan hatası, taksit durumu ("Gecikti" — renk tek başına anlam taşımaz) ve ödeme
// yöntemi adları tr.json'dan; sunucunun Türkiye günüyle gelen anlar Türkiye saat dilimiyle gösterilir.

type FieldScope = "name" | "amount" | "percent" | "count" | "due" | "date" | "method" | "note" | "reason" | "fee";

export function billingFieldText(error: BillingFieldError, scope: FieldScope): string {
  if (error === "required") {
    const required: Partial<Record<FieldScope, TextKey>> = {
      due: "billing.field.dueRequired",
      date: "billing.field.dateRequired",
      reason: "billing.field.reasonRequired",
      fee: "billing.field.feeRequired",
      method: "billing.field.methodRequired",
    };
    return t(required[scope] ?? "billing.field.required");
  }

  if (error === "tooLong") {
    return t(scope === "note" ? "billing.field.noteTooLong" : scope === "reason" ? "billing.field.reasonTooLong" : "billing.field.nameTooLong");
  }

  return t(`billing.field.${error}` as TextKey);
}

export function stateText(state: string): string {
  return isInstallmentState(state) ? t(`billing.state.${state}`) : state;
}

export function methodText(method: string): string {
  return isPaymentMethod(method) ? t(`billing.method.${method}`) : method;
}

export function fill(key: TextKey, values: Record<string, string | number>): string {
  return fillText(t(key), values);
}

/** Sunucu anını (UTC) Türkiye günü olarak "gg.aa.yyyy". */
export function turkeyDate(instant: string): string {
  return new Intl.DateTimeFormat("tr-TR", { timeZone: "Europe/Istanbul", day: "2-digit", month: "2-digit", year: "numeric" }).format(new Date(instant));
}
