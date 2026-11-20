import { t } from "../../i18n/t";
import { AccountPage } from "../account/AccountPage";
import { AdminPage } from "../admin/AdminPage";
import { resetTokenFromRoute } from "@okul/api-client";
import { ForgotPasswordScreen } from "./ForgotPasswordScreen";
import { ResetPasswordScreen } from "./ResetPasswordScreen";
import { useHashRoute } from "../../routing/useHashRoute";
import { LoginScreen } from "./LoginScreen";
import { SchoolSelectScreen } from "./SchoolSelectScreen";
import { useAuthController } from "./useAuthController";
import { WelcomeScreen } from "./WelcomeScreen";

/**
 * Oturum durumuna göre ekranı seçer: kontrol ediliyor → giriş → okul seç → hoş geldin (spec 0004, web); "#/yonetim"
 * adresinde yönetim sayfası (spec 0005); "#/hesabim" Hesabım, "#/sifremi-unuttum" ve "#/sifre-yenile" şifre işlemleri
 * (spec 0006). Kısa geçiş
 * yazıları canlı bölge değildir (sayfadaki tek durum duyurusu sistem durumu kartıdır); hatalar role=alert ile duyurulur.
 */
export function AuthGate() {
  const { state, session, login, retryResume, selectSchool, switchSchool, logout, reloadMe } = useAuthController();
  const { route, navigate } = useHashRoute();
  const path = route.split("?")[0];

  // Şifremi unuttum ve bağlantıyla yenileme oturumdan bağımsız (spec 0006).
  if (path === "/sifremi-unuttum") {
    return <ForgotPasswordScreen />;
  }

  if (path === "/sifre-yenile") {
    return (
      <ResetPasswordScreen
        token={resetTokenFromRoute(route)}
        onDone={() => {
          navigate("/");
          void logout();
        }}
      />
    );
  }

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
      if (path === "/hesabim") {
        return <AccountPage client={session.client} onProfileSaved={() => void reloadMe()} />;
      }

      return path === "/yonetim" ? (
        <AdminPage me={state.me} client={session.client} />
      ) : (
        <WelcomeScreen me={state.me} onSwitchSchool={switchSchool} onLogout={logout} />
      );
  }
}
