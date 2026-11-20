// Giriş akışının durum makinesi (web ve mobil aynı; spec 0004 "User experience"): oturum kontrol ediliyor → giriş
// (boş / gönderiliyor / hata / kilit / ulaşılamıyor / oturum doldu) → okul seç (birden çok okul) → hoş geldin.
// Ekranlar yalnızca bu durumu çizer; ağ işi AuthSession'dadır. "ulaşılamıyor" açılıştan/oturumdan geldiyse
// (resume: true) "Tekrar dene" kayıtlı oturumu yeniden dener; girişten geldiyse formu yeniden gönderir (review L-8).
import type { AuthOutcome, AuthSession, Me, TenantSummary } from "./session";

export type LoginProblem =
  | { kind: "server"; message: string }
  | { kind: "invalid" }
  | { kind: "unreachable"; resume?: boolean };

export type AuthState =
  | { kind: "restoring" }
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
