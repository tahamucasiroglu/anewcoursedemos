import { t } from "../../i18n/t";
import { LoginScreen } from "./LoginScreen";
import { SchoolSelectScreen } from "./SchoolSelectScreen";
import { useAuthController } from "./useAuthController";
import { WelcomeScreen } from "./WelcomeScreen";

/**
 * Oturum durumuna göre ekranı seçer: kontrol ediliyor → giriş → okul seç → hoş geldin (spec 0004, web). Kısa geçiş
 * yazıları canlı bölge değildir (sayfadaki tek durum duyurusu sistem durumu kartıdır); hatalar role=alert ile duyurulur.
 */
export function AuthGate() {
  const { state, login, retryResume, selectSchool, switchSchool, logout } = useAuthController();

  switch (state.kind) {
    case "restoring":
      return <p className="notice" aria-busy="true">{t("auth.restoring")}</p>;
    case "signedOut":
      return <LoginScreen submitting={state.submitting} expired={state.expired} problem={state.problem} onSubmit={login} onRetryResume={retryResume} />;
    case "selectingSchool":
      return <SchoolSelectScreen tenants={state.tenants} selecting={state.selecting} failed={state.failed} onSelect={selectSchool} />;
    case "loadingMe":
      return <p className="notice" aria-busy="true">{t("welcome.loading")}</p>;
    case "signedIn":
      return <WelcomeScreen me={state.me} onSwitchSchool={switchSchool} onLogout={logout} />;
  }
}
