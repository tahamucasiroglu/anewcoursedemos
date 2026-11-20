// Hesabım ve şifre formlarının alan kuralları (spec 0006): sunucudaki kurallarla aynı; hatalı alan varsa istek gönderilmez.
// Sonuç metin değil anahtardır; ekran metni kendi tr.json'undan alır.
import { isValidPhone } from "../admin/adminForms";
import type { components } from "../schema";

export type MyProfile = components["schemas"]["MyProfile"];

export type AccountFieldError =
  | "fullNameRequired"
  | "nameTooLong"
  | "phoneInvalid"
  | "currentRequired"
  | "passwordRequired"
  | "passwordTooShort"
  | "passwordSame"
  | "passwordMismatch"
  | "emailRequired"
  | "emailInvalid";

export const PASSWORD_MIN_LENGTH = 8;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export interface ProfileFormValues {
  fullName: string;
  phone: string;
}

export function validateProfileForm(values: ProfileFormValues): Partial<Record<keyof ProfileFormValues, AccountFieldError>> {
  const errors: Partial<Record<keyof ProfileFormValues, AccountFieldError>> = {};
  const name = values.fullName.trim();
  if (name === "") {
    errors.fullName = "fullNameRequired";
  } else if (name.length > 100) {
    errors.fullName = "nameTooLong";
  }

  if (!isValidPhone(values.phone)) {
    errors.phone = "phoneInvalid";
  }

  return errors;
}

export type NewPasswordErrors = Partial<Record<"next" | "repeat", AccountFieldError>>;

/** Yeni şifre + tekrarı (bağlantıyla yenileme ve şifre değiştirme ortak kuralı). */
export function validateNewPassword(next: string, repeat: string): NewPasswordErrors {
  const errors: NewPasswordErrors = {};
  if (next === "") {
    errors.next = "passwordRequired";
  } else if (next.length < PASSWORD_MIN_LENGTH) {
    errors.next = "passwordTooShort";
  }

  if (repeat !== next) {
    errors.repeat = "passwordMismatch";
  }

  return errors;
}

export interface PasswordChangeValues {
  current: string;
  next: string;
  repeat: string;
}

export type PasswordChangeErrors = Partial<Record<keyof PasswordChangeValues, AccountFieldError>>;

export function validatePasswordChange(values: PasswordChangeValues): PasswordChangeErrors {
  const errors: PasswordChangeErrors = { ...validateNewPassword(values.next, values.repeat) };
  if (values.current === "") {
    errors.current = "currentRequired";
  } else if (!errors.next && values.next === values.current) {
    errors.next = "passwordSame";
  }

  return errors;
}

export function validateEmailField(email: string): AccountFieldError | null {
  const trimmed = email.trim();
  if (trimmed === "") {
    return "emailRequired";
  }

  return EMAIL_PATTERN.test(trimmed) ? null : "emailInvalid";
}

/** Bağlantı adresindeki anahtar: "#/sifre-yenile?anahtar=…" → anahtar (yoksa boş). */
export function resetTokenFromRoute(route: string): string {
  const query = route.split("?")[1] ?? "";
  return new URLSearchParams(query).get("anahtar") ?? "";
}
