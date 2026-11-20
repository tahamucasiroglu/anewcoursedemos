// Platform paneli (spec 0007): okul ekle formu ve 6 haneli kod kuralları — sunucudaki kurallarla aynı; hatalı alan varsa
// istek gönderilmez. Sonuç metin değil anahtardır; ekran metni kendi tr.json'undan alır.
import type { components } from "../schema";

export type SchoolPage = components["schemas"]["SchoolPage"];
export type SchoolRow = components["schemas"]["SchoolRow"];
export type PanelSchoolResult = components["schemas"]["PanelSchoolResult"];

export type PlatformFieldError =
  | "schoolNameRequired"
  | "branchNameRequired"
  | "founderNameRequired"
  | "nameTooLong"
  | "emailRequired"
  | "emailInvalid"
  | "codeRequired";

export interface SchoolFormValues {
  schoolName: string;
  branchName: string;
  founderName: string;
  founderEmail: string;
}

export type SchoolFormErrors = Partial<Record<keyof SchoolFormValues, PlatformFieldError>>;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function name(value: string, required: PlatformFieldError): PlatformFieldError | undefined {
  const trimmed = value.trim();
  return trimmed === "" ? required : trimmed.length > 100 ? "nameTooLong" : undefined;
}

export function validateSchoolForm(values: SchoolFormValues): SchoolFormErrors {
  const errors: SchoolFormErrors = {
    schoolName: name(values.schoolName, "schoolNameRequired"),
    branchName: name(values.branchName, "branchNameRequired"),
    founderName: name(values.founderName, "founderNameRequired"),
    founderEmail: values.founderEmail.trim() === "" ? "emailRequired" : EMAIL_PATTERN.test(values.founderEmail.trim()) ? undefined : "emailInvalid",
  };
  return Object.fromEntries(Object.entries(errors).filter(([, v]) => v !== undefined)) as SchoolFormErrors;
}

/** 6 haneli kod (boşluklar yok sayılır); geçerliyse null. */
export function validateCode(code: string): PlatformFieldError | null {
  return /^\d{6}$/.test(code.replace(/\s/g, "")) ? null : "codeRequired";
}

export function normalizeCode(code: string): string {
  return code.replace(/\s/g, "");
}
