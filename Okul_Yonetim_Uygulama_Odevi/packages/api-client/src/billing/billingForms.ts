// Tahsilat ekranlarının ortak mantığı (spec 0015): tutar ayrıştırma ("1.234,50" → sunucuya giden "1234.50" metni ve kuruş tamsayısı;
// kayan nokta yok), yüzde, tarife/indirim/plan/tahsilat/iptal formlarının istemci doğrulaması (sınırlar sunucuyla aynı; sunucu yine
// ayrıca doğrular), taksit durumu ve ödeme yöntemi adları, gün yardımcıları. Metinler ekranın tr.json'undan gelir.
import { addDays } from "../academics/academicsForms";
import { toCents, type Amount } from "./moneyText";
import type { components } from "../schema";

export type FeeScheduleRow = components["schemas"]["FeeScheduleRow"];
export type DiscountRow = components["schemas"]["DiscountRow"];
export type PlanPreview = components["schemas"]["PlanPreview"];
export type InstallmentLine = components["schemas"]["InstallmentLine"];
export type StudentAccountPage = components["schemas"]["StudentAccountPage"];
export type StudentAccountRow = components["schemas"]["StudentAccountRow"];
export type StudentAccount = components["schemas"]["StudentAccount"];
export type AccountTotals = components["schemas"]["AccountTotals"];
export type PlanView = components["schemas"]["PlanView"];
export type InstallmentView = components["schemas"]["InstallmentView"];
export type PaymentRow = components["schemas"]["PaymentRow"];
export type LedgerMovement = components["schemas"]["LedgerMovement"];
export type PaymentRecorded = components["schemas"]["PaymentRecorded"];
export type Receipt = components["schemas"]["Receipt"];
export type ReceiptAllocation = components["schemas"]["ReceiptAllocation"];
export type MyPayments = components["schemas"]["MyPayments"];
export type ChildAccount = components["schemas"]["ChildAccount"];
export type ChildPlan = components["schemas"]["ChildPlan"];
export type ChildPayment = components["schemas"]["ChildPayment"];

export const BILLING_NAME_MAX = 100;
export const MAX_FEE_CENTS = 1_000_000_000;
export const NOTE_MAX = 200;
export const REASON_MAX = 200;
export const MIN_INSTALLMENTS = 1;
export const MAX_INSTALLMENTS = 12;
export const FIRST_DUE_MAX_DAYS = 365;
export const PAYMENT_MAX_PAST_DAYS = 365;
export const ACCOUNTS_PAGE_SIZE = 25;

export type PaymentMethod = "Cash" | "BankTransfer" | "CardPos";
export const PAYMENT_METHODS: readonly PaymentMethod[] = ["Cash", "BankTransfer", "CardPos"];
export type InstallmentState = "Paid" | "PartiallyPaid" | "Pending" | "Overdue";

export type BillingFieldError =
  | "required"
  | "tooLong"
  | "amountInvalid"
  | "amountNotPositive"
  | "amountDecimals"
  | "amountTooLarge"
  | "amountOverRemaining"
  | "percentInvalid"
  | "countInvalid"
  | "dueOutOfRange"
  | "dateInFuture"
  | "dateTooOld"
  | "methodRequired";

export type AmountParse = { ok: true; text: string; cents: number } | { ok: false; error: BillingFieldError };

/**
 * Kullanıcının yazdığı tutar: "1.234,50", "1234,5", "1234" ya da "1234.50" (tek noktalı ve en fazla iki ondalıklı yazım ondalık
 * sayılır; "1.234" binlik ayırıcıdır). Sonuç sunucuya giden metin ("1234.50") ve kuruş tamsayısı; kayan noktalı sayı kullanılmaz.
 */
export function parseAmount(input: string): AmountParse {
  const raw = input.trim().replace(/\s/g, "");
  if (raw.length === 0) {
    return { ok: false, error: "required" };
  }

  let normalized: string;
  if (raw.includes(",")) {
    if (!/^-?\d{1,3}(\.\d{3})*(,\d*)?$/.test(raw) && !/^-?\d+(,\d*)?$/.test(raw)) {
      return { ok: false, error: "amountInvalid" };
    }

    normalized = raw.replace(/\./g, "").replace(",", ".");
  } else if (/^-?\d{1,3}(\.\d{3})+$/.test(raw)) {
    normalized = raw.replace(/\./g, "");
  } else if (/^-?\d+(\.\d+)?$/.test(raw)) {
    normalized = raw;
  } else {
    return { ok: false, error: "amountInvalid" };
  }

  const negative = normalized.startsWith("-");
  const [whole, fraction = ""] = normalized.replace("-", "").split(".");
  if (whole.length > 13) {
    return { ok: false, error: "amountTooLarge" };
  }

  const significant = fraction.replace(/0+$/, "");
  if (significant.length > 2) {
    return { ok: false, error: "amountDecimals" };
  }

  const cents = Number(whole) * 100 + Number(significant.padEnd(2, "0"));
  if (negative || cents <= 0) {
    return { ok: false, error: "amountNotPositive" };
  }

  return { ok: true, text: `${Math.floor(cents / 100)}.${(cents % 100).toString().padStart(2, "0")}`, cents };
}

/** Yüzde: 1–100 tam sayı. */
export function parsePercent(input: string): { ok: true; value: number } | { ok: false; error: BillingFieldError } {
  const raw = input.trim();
  if (raw.length === 0) {
    return { ok: false, error: "required" };
  }

  if (!/^\d{1,3}$/.test(raw) || Number(raw) < 1 || Number(raw) > 100) {
    return { ok: false, error: "percentInvalid" };
  }

  return { ok: true, value: Number(raw) };
}

function nameError(name: string): BillingFieldError | undefined {
  const clean = name.trim();
  return clean.length === 0 ? "required" : clean.length > BILLING_NAME_MAX ? "tooLong" : undefined;
}

export interface TariffFormValues { name: string; amount: string }
export interface TariffFormErrors { name?: BillingFieldError; amount?: BillingFieldError }

export function validateTariffForm(values: TariffFormValues): TariffFormErrors {
  const errors: TariffFormErrors = {};
  const name = nameError(values.name);
  if (name) {
    errors.name = name;
  }

  const amount = parseAmount(values.amount);
  if (!amount.ok) {
    errors.amount = amount.error;
  } else if (amount.cents > MAX_FEE_CENTS) {
    errors.amount = "amountTooLarge";
  }

  return errors;
}

export interface DiscountFormValues { name: string; percent: string }
export interface DiscountFormErrors { name?: BillingFieldError; percent?: BillingFieldError }

export function validateDiscountForm(values: DiscountFormValues): DiscountFormErrors {
  const errors: DiscountFormErrors = {};
  const name = nameError(values.name);
  if (name) {
    errors.name = name;
  }

  const percent = parsePercent(values.percent);
  if (!percent.ok) {
    errors.percent = percent.error;
  }

  return errors;
}

export interface PlanFormValues { feeScheduleId: string; discountId: string; installmentCount: string; firstDueDate: string }
export interface PlanFormErrors { feeScheduleId?: BillingFieldError; installmentCount?: BillingFieldError; firstDueDate?: BillingFieldError }

/** Plan formu: tarife zorunlu, taksit sayısı 1–12, ilk vade bugün ile 365 gün sonrası arasında (gün metinleri "yyyy-aa-gg"). */
export function validatePlanForm(values: PlanFormValues, today: string): PlanFormErrors {
  const errors: PlanFormErrors = {};
  if (values.feeScheduleId === "") {
    errors.feeScheduleId = "required";
  }

  const count = Number(values.installmentCount);
  if (!/^\d{1,2}$/.test(values.installmentCount) || count < MIN_INSTALLMENTS || count > MAX_INSTALLMENTS) {
    errors.installmentCount = "countInvalid";
  }

  if (!isIsoDay(values.firstDueDate)) {
    errors.firstDueDate = "required";
  } else if (values.firstDueDate < today || values.firstDueDate > addDays(today, FIRST_DUE_MAX_DAYS)) {
    errors.firstDueDate = "dueOutOfRange";
  }

  return errors;
}

/** Plan formundan istek gövdesi (doğrulanmış değerler). */
export function planBody(studentId: string, values: PlanFormValues) {
  return {
    studentId,
    feeScheduleId: values.feeScheduleId,
    discountId: values.discountId === "" ? null : values.discountId,
    installmentCount: Number(values.installmentCount),
    firstDueDate: values.firstDueDate,
  };
}

export interface PaymentFormValues { amount: string; date: string; method: PaymentMethod | ""; note: string }
export interface PaymentFormErrors { amount?: BillingFieldError; date?: BillingFieldError; method?: BillingFieldError; note?: BillingFieldError }

/** Tahsilat formu: tutar (kalan borcu aşamaz — kuruşla), tarih bugün ya da en fazla 365 gün önce, yöntem zorunlu, not en fazla 200. */
export function validatePaymentForm(values: PaymentFormValues, today: string, remaining: Amount | null): PaymentFormErrors {
  const errors: PaymentFormErrors = {};
  const amount = parseAmount(values.amount);
  if (!amount.ok) {
    errors.amount = amount.error;
  } else if (remaining !== null && amount.cents > toCents(remaining)) {
    errors.amount = "amountOverRemaining";
  }

  if (!isIsoDay(values.date)) {
    errors.date = "required";
  } else if (values.date > today) {
    errors.date = "dateInFuture";
  } else if (values.date < addDays(today, -PAYMENT_MAX_PAST_DAYS)) {
    errors.date = "dateTooOld";
  }

  if (values.method === "") {
    errors.method = "methodRequired";
  }

  if (values.note.trim().length > NOTE_MAX) {
    errors.note = "tooLong";
  }

  return errors;
}

/** Tahsilat formundan istek gövdesi: tutar metin olarak ("1234.50") gider. */
export function paymentBody(planId: string, values: PaymentFormValues) {
  const amount = parseAmount(values.amount);
  return {
    planId,
    amount: amount.ok ? amount.text : values.amount,
    date: values.date,
    method: values.method === "" ? null : values.method,
    note: values.note.trim() === "" ? null : values.note.trim(),
  };
}

/** İptal gerekçesi: zorunlu, en fazla 200. */
export function validateCancelReason(reason: string): BillingFieldError | undefined {
  const clean = reason.trim();
  return clean.length === 0 ? "required" : clean.length > REASON_MAX ? "tooLong" : undefined;
}

export function isIsoDay(value: string): boolean {
  return /^\d{4}-\d{2}-\d{2}$/.test(value) && !Number.isNaN(Date.parse(`${value}T00:00:00Z`));
}


/** Cihazın yerel günü "yyyy-aa-gg" (istemci ön doğrulaması için; asıl sınır sunucunun Türkiye günü). */
export function localToday(now: Date = new Date()): string {
  const month = (now.getMonth() + 1).toString().padStart(2, "0");
  const day = now.getDate().toString().padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

export function isInstallmentState(value: string): value is InstallmentState {
  return value === "Paid" || value === "PartiallyPaid" || value === "Pending" || value === "Overdue";
}

export function isPaymentMethod(value: string): value is PaymentMethod {
  return (PAYMENT_METHODS as readonly string[]).includes(value);
}
