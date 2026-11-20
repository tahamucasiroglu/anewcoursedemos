import { useCallback, useEffect, useState } from "react";
import { AuthSession, afterAuth, afterResume, initialAuthState, loadMe, signedOut, type AuthState } from "@okul/api-client";
import { apiBaseUrl } from "../../config";

/**
 * Web oturum denetleyicisi (spec 0004): açılışta çerezdeki oturumu sessizce dener; giriş, okul seçimi, okul değiştirme
 * ve çıkışı yürütür. Yenileme anahtarı HttpOnly çerezde (JavaScript görmez); erişim anahtarı yalnızca bellekte.
 * Oturum herhangi bir istekte yenilenemezse giriş ekranına "oturum doldu" ile dönülür.
 */
export function useAuthController() {
  const [state, setState] = useState<AuthState>(initialAuthState);
  const [session] = useState(
    () => new AuthSession({ baseUrl: apiBaseUrl, delivery: "cookie", onExpired: () => setState(signedOut(true)) }),
  );

  const proceed = useCallback(
    async (next: AuthState) => {
      setState(next);
      if (next.kind === "loadingMe") {
        setState(await loadMe(session));
      }
    },
    [session],
  );

  useEffect(() => {
    let active = true;
    session.resume().then((outcome) => {
      if (active) {
        void proceed(afterResume(outcome));
      }
    });
    return () => {
      active = false;
    };
  }, [session, proceed]);

  // Açılışta/oturumda sunucuya ulaşılamadıysa "Tekrar dene": kayıtlı oturumu yeniden dener (review L-8).
  const retryResume = useCallback(async () => {
    setState(initialAuthState);
    await proceed(afterResume(await session.resume()));
  }, [session, proceed]);

  const login = useCallback(
    async (email: string, password: string) => {
      setState({ kind: "signedOut", expired: false, submitting: true, problem: null });
      await proceed(afterAuth(await session.login(email, password)));
    },
    [session, proceed],
  );

  const selectSchool = useCallback(
    async (tenantId: string) => {
      setState((current) => (current.kind === "selectingSchool" ? { ...current, selecting: true, failed: false } : current));
      const outcome = await session.selectTenant(tenantId);
      if (outcome.kind === "ok") {
        await proceed({ kind: "loadingMe" });
      } else {
        setState((current) => (current.kind === "selectingSchool" ? { ...current, selecting: false, failed: true } : current));
      }
    },
    [session, proceed],
  );

  const switchSchool = useCallback(() => {
    setState((current) =>
      current.kind === "signedIn" ? { kind: "selectingSchool", tenants: current.me.tenants, selecting: false, failed: false } : current,
    );
  }, []);

  const logout = useCallback(async () => {
    await session.logout();
    setState(signedOut());
  }, [session]);

  return { state, session, login, retryResume, selectSchool, switchSchool, logout };
}
