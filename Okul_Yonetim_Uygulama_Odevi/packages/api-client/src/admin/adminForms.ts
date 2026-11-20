// Yönetim formlarının alan kuralları (spec 0005): sunucudaki kurallarla aynı; hatalı alan varsa istek gönderilmez.
// Sonuç metin değil anahtardır; ekran metni kendi tr.json'undan alır.
import type { components } from "../schema";

export type BranchAdminView = components["schemas"]["BranchAdminView"];
export type MemberRow = components["schemas"]["MemberRow"];
export type MemberDetail = components["schemas"]["MemberDetail"];
export type MemberPage = components["schemas"]["MemberPage"];
export type AddMemberResult = components["schemas"]["AddMemberResult"];
export type RoleInput = components["schemas"]["RoleInput"];

export type RoleName = "Founder" | "Teacher" | "Guardian" | "Accounting";
export const ROLE_NAMES: readonly RoleName[] = ["Founder", "Teacher", "Guardian", "Accounting"];

export type AdminFieldError =
  | "nameRequired"
  | "nameTooLong"
  | "addressTooLong"
  | "phoneInvalid"
  | "fullNameRequired"
  | "emailRequired"
  | "emailInvalid"
  | "roleRequired"
  | "branchRequired";

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Türkiye numarası (sunucudaki PhoneNumber ile aynı kural): boş geçerli; 10 haneli ulusal numara 2–5 ile başlar. */
export function isValidPhone(value: string | null | undefined): boolean {
  const trimmed = (value ?? "").trim();
  if (trimmed === "") {
    return true;
  }

  if (/[^\d\s\-().+]/.test(trimmed)) {
    return false;
  }

  const digits = trimmed.replace(/\D/g, "");
  const national = trimmed.startsWith("+")
    ? digits.startsWith("90") ? digits.slice(2) : ""
    : digits.startsWith("90") && digits.length === 12 ? digits.slice(2)
    : digits.startsWith("0") ? digits.slice(1)
    : digits;
  return national.length === 10 && national[0] >= "2" && national[0] <= "5";
}

export interface BranchFormValues {
  name: string;
  address: string;
  phone: string;
}

export function validateBranchForm(values: BranchFormValues): Partial<Record<keyof BranchFormValues, AdminFieldError>> {
  const errors: Partial<Record<keyof BranchFormValues, AdminFieldError>> = {};
  const name = values.name.trim();
  if (name === "") {
    errors.name = "nameRequired";
  } else if (name.length > 100) {
    errors.name = "nameTooLong";
  }

  if (values.address.trim().length > 300) {
    errors.address = "addressTooLong";
  }

  if (!isValidPhone(values.phone)) {
    errors.phone = "phoneInvalid";
  }

  return errors;
}

export interface MemberFormValues {
  fullName: string;
  email: string;
  phone: string;
  roles: RoleInput[];
}

export type MemberFormErrors = Partial<Record<"fullName" | "email" | "phone" | "roles", AdminFieldError>>;

/** <c>requireEmail</c>: ekleme formunda e-posta zorunlu; düzenlemede e-posta değişmez, denetlenmez. */
export function validateMemberForm(values: MemberFormValues, requireEmail = true): MemberFormErrors {
  const errors: MemberFormErrors = {};
  const fullName = values.fullName.trim();
  if (fullName === "") {
    errors.fullName = "fullNameRequired";
  } else if (fullName.length > 100) {
    errors.fullName = "nameTooLong";
  }

  if (requireEmail) {
    const email = values.email.trim();
    if (email === "") {
      errors.email = "emailRequired";
    } else if (!EMAIL_PATTERN.test(email)) {
      errors.email = "emailInvalid";
    }

    if (values.roles.length === 0) {
      errors.roles = "roleRequired";
    }
  }

  if (!isValidPhone(values.phone)) {
    errors.phone = "phoneInvalid";
  }

  if (values.roles.some((r) => r.role !== "Founder" && r.branchIds.length === 0)) {
    errors.roles = "branchRequired";
  }

  return errors;
}
