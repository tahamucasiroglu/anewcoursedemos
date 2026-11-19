// Giriş formunun alan kontrolü (web ve mobil aynı kural; spec 0004 AC-5): hatalı alan varsa istek gönderilmez.
// Sonuç metin değil anahtardır; ekran kendi tr.json'undan metni alır.
export type LoginFieldError = "emailRequired" | "emailInvalid" | "passwordRequired";

export interface LoginFormErrors {
  email?: LoginFieldError;
  password?: LoginFieldError;
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateLoginForm(email: string, password: string): LoginFormErrors {
  const errors: LoginFormErrors = {};
  const trimmed = email.trim();
  if (trimmed === "") {
    errors.email = "emailRequired";
  } else if (!EMAIL_PATTERN.test(trimmed)) {
    errors.email = "emailInvalid";
  }

  if (password === "") {
    errors.password = "passwordRequired";
  }

  return errors;
}
