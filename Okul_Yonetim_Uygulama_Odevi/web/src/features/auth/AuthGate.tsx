import { t } from "../../i18n/t";
import { AccountPage } from "../account/AccountPage";
import { AttendancePage } from "../attendance/AttendancePage";
import { AdminPage } from "../admin/AdminPage";
import { BillingPage } from "../billing/BillingPage";
import { ReceiptPage } from "../billing/ReceiptPage";
import { CalendarPage } from "../calendar/CalendarPage";
import { CommunicationPage } from "../communication/CommunicationPage";
import { DashboardPage } from "../dashboard/DashboardPage";
import { NoticesPage } from "../notices/NoticesPage";
import { PlatformPage } from "../platform/PlatformPage";
import { StudentsPage } from "../students/StudentsPage";
import { WeeklySummaryPage } from "../weekly-summary/WeeklySummaryPage";
import { resetTokenFromRoute } from "@okul/api-client";
import { ForgotPasswordScreen } from "./ForgotPasswordScreen";
import { ResetPasswordScreen } from "./ResetPasswordScreen";
import { useHashRoute } from "../../routing/useHashRoute";
import { LoginScreen } from "./LoginScreen";
import { SchoolSelectScreen } from "./SchoolSelectScreen";
import { TwoFactorScreen } from "./TwoFactorScreen";
import { TwoFactorSetupScreen } from "./TwoFactorSetupScreen";
import { useAuthController } from "./useAuthController";
import { WelcomeScreen } from "./WelcomeScreen";

/**
 * Oturum durumuna göre ekranı seçer: kontrol ediliyor → giriş → okul seç → hoş geldin (spec 0004, web); "#/yonetim"
 * adresinde yönetim sayfası (spec 0005); "#/hesabim" Hesabım, "#/sifremi-unuttum" ve "#/sifre-yenile" şifre işlemleri
 * (spec 0006); platform yöneticisinde iki adımlı kod/kurulum adımı ve "#/platform" paneli (spec 0007); "#/takvim" okul
 * takvimi (spec 0008, her rol); "#/ogrenciler" öğrenci yönetimi (spec 0009, yalnızca kurucu); "#/yoklama" yoklama (öğretmen) ve
 * "#/bildirimler" veli bildirimleri (spec 0010; okununca kişi bilgisi — okunmamış sayısı — yenilenir); "#/iletisim" duyurular
 * (spec 0011, kurucu ve öğretmen); "#/haftalik-ozet" haftalık gelişim özeti (spec 0014, sınıf öğretmeni); "#/tahsilat" tahsilat ve öğrenci carisi, "#/makbuz/&lt;id&gt;" yazdırılabilir makbuz (spec 0015, kurucu ve muhasebe); "#/panel" ve "#/panel/raporlar" yönetici paneli, "#/iletisim/duyuru/&lt;id&gt;" duyuru ayrıntısı (spec 0017, kurucu). Kısa geçiş
 * yazıları canlı bölge değildir (sayfadaki tek durum duyurusu sistem durumu kartıdır); hatalar role=alert ile duyurulur.
 */
export function AuthGate() {
  const { state, session, login, retryResume, selectSchool, switchSchool, logout, reloadMe, submitCode, restart } = useAuthController();
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
    case "twoFactor":
      return state.setupRequired ? (
        <TwoFactorSetupScreen session={session} challenge={state.challenge} submitting={state.submitting} problem={state.problem} onSubmit={submitCode} onRestart={restart} />
      ) : (
        <TwoFactorScreen submitting={state.submitting} problem={state.problem} onSubmit={submitCode} onRestart={restart} />
      );
    case "selectingSchool":
      return <SchoolSelectScreen tenants={state.tenants} selecting={state.selecting} failed={state.failed} onSelect={selectSchool} />;
    case "loadingMe":
      return <p className="notice" aria-busy="true">{t("welcome.loading")}</p>;
    case "signedIn":
      if (path === "/hesabim") {
        return <AccountPage client={session.client} onProfileSaved={() => void reloadMe()} />;
      }

      if (path === "/takvim") {
        return <CalendarPage client={session.client} />;
      }

      if (path === "/yoklama") {
        return <AttendancePage me={state.me} client={session.client} />;
      }

      if (path === "/bildirimler") {
        return <NoticesPage me={state.me} client={session.client} onRead={() => void reloadMe()} />;
      }

      if (path === "/iletisim" || path.startsWith("/iletisim/duyuru/")) {
        return <CommunicationPage key={path} me={state.me} client={session.client} announcementId={path.startsWith("/iletisim/duyuru/") ? path.slice("/iletisim/duyuru/".length) : undefined} />;
      }

      if (path === "/panel" || path === "/panel/raporlar") {
        return <DashboardPage me={state.me} client={session.client} route={route} />;
      }

      if (path === "/haftalik-ozet") {
        return <WeeklySummaryPage me={state.me} client={session.client} />;
      }

      if (path === "/tahsilat" || path.startsWith("/tahsilat/")) {
        return <BillingPage me={state.me} client={session.client} route={route} />;
      }

      if (path.startsWith("/makbuz/")) {
        return <ReceiptPage key={path} me={state.me} client={session.client} paymentId={path.slice("/makbuz/".length)} />;
      }

      if (path === "/ogrenciler") {
        return <StudentsPage me={state.me} client={session.client} />;
      }

      if (path === "/platform") {
        return <PlatformPage me={state.me} client={session.client} />;
      }

      return path === "/yonetim" ? (
        <AdminPage me={state.me} client={session.client} />
      ) : (
        <WelcomeScreen me={state.me} onSwitchSchool={switchSchool} onLogout={logout} />
      );
  }
}
