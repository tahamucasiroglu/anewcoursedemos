// Giriş akışının durum makinesi (web ve mobil aynı; spec 0004 "User experience"): oturum kontrol ediliyor → giriş
// (boş / gönderiliyor / hata / kilit / ulaşılamıyor / oturum doldu) → [platform yöneticisi: iki adımlı kod, spec 0007]
// → okul seç (birden çok okul) → hoş geldin.
// Ekranlar yalnızca bu durumu çizer; ağ işi AuthSession'dadır. "ulaşılamıyor" açılıştan/oturumdan geldiyse
// (resume: true) "Tekrar dene" kayıtlı oturumu yeniden dener; girişten geldiyse formu yeniden gönderir (review L-8).
import type { AuthOutcome, AuthSession, Me, TenantSummary } from "./session";

export type LoginProblem =
  | { kind: "server"; message: string }
  | { kind: "invalid" }
  | { kind: "unreachable"; resume?: boolean };

/** Kod adımı sorunu: sunucu mesajı ("Kod hatalı", kilit; expired: bekleme süresi doldu → girişe dön) ya da ulaşılamıyor. */
export type TwoFactorProblem = { kind: "server"; message: string | null; expired: boolean } | { kind: "unreachable" };

export type AuthState =
  | { kind: "restoring" }
  | { kind: "twoFactor"; challenge: string; setupRequired: boolean; submitting: boolean; problem: TwoFactorProblem | null }
  | { kind: "signedOut"; expired: boolean; submitting: boolean; problem: LoginProblem | null }
  | { kind: "selectingSchool"; tenants: TenantSummary[]; selecting: boolean; failed: boolean }
  | { kind: "loadingMe" }
  | { kind: "signedIn"; me: Me };

export const initialAuthState: AuthState = { kind: "restoring" };

export function signedOut(expired = false): AuthState {
  return { kind: "signedOut", expired, submitting: false, problem: null };
}

/** Giriş/yenileme sonucunu bir sonraki duruma çevirir; "ok" ise okul seçimi gerekip gerekmediğine bakar. */
export function afterAuth(outcome: AuthOutcome): AuthState {
  if (outcome.kind === "twoFactor") {
    return { kind: "twoFactor", challenge: outcome.challenge, setupRequired: outcome.setupRequired, submitting: false, problem: null };
  }

  if (outcome.kind === "ok") {
    return outcome.tokens.requiresTenantSelection
      ? { kind: "selectingSchool", tenants: outcome.tokens.tenants, selecting: false, failed: false }
      : { kind: "loadingMe" };
  }

  const problem: LoginProblem =
    outcome.kind === "unreachable"
      ? { kind: "unreachable" }
      : outcome.message
        ? { kind: "server", message: outcome.message }
        : { kind: "invalid" };
  return { kind: "signedOut", expired: false, submitting: false, problem };
}

/**
 * Açılıştaki sessiz yenilemenin sonucu: geçerli oturum → devam; sunucuya ulaşılamıyor → giriş ekranında "ulaşılamıyor"
 * (kayıtlı oturum silinmez, tekrar açılınca denenir); oturum yok/geçersiz → boş giriş ekranı.
 */
export function afterResume(outcome: AuthOutcome): AuthState {
  if (outcome.kind === "unreachable") {
    return { kind: "signedOut", expired: false, submitting: false, problem: { kind: "unreachable", resume: true } };
  }

  return outcome.kind === "rejected" ? signedOut() : afterAuth(outcome);
}

/** Oturumu kullanan ekranların ortak işlemleri; React tarafı (web/mobil) bunları setState ile bağlar. */
export async function loadMe(session: AuthSession): Promise<AuthState> {
  try {
    const me = await session.me();
    return me ? { kind: "signedIn", me } : signedOut(true);
  } catch {
    // Ağ hatası oturumun bittiği anlamına gelmez (review L-2).
    return { kind: "signedOut", expired: false, submitting: false, problem: { kind: "unreachable", resume: true } };
  }
}

/** Kod adımının sonucu (spec 0007): doğru kod → oturum akışı; ret → aynı adımda mesaj (401: bekleme bitti); ağ hatası. */
export function afterTwoFactor(state: Extract<AuthState, { kind: "twoFactor" }>, outcome: AuthOutcome): AuthState {
  if (outcome.kind === "ok" || outcome.kind === "twoFactor") {
    return afterAuth(outcome);
  }

  const problem: TwoFactorProblem =
    outcome.kind === "unreachable" ? { kind: "unreachable" } : { kind: "server", message: outcome.message, expired: outcome.status === 401 };
  return { ...state, submitting: false, problem };
}
