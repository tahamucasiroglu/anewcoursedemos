import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { t } from "../../i18n/t";
import { AccountScreen } from "../account/AccountScreen";
import { CalendarScreen } from "../calendar/CalendarScreen";
import { ForgotPasswordScreen } from "./ForgotPasswordScreen";
import { LoginScreen } from "./LoginScreen";
import { SchoolSelectScreen } from "./SchoolSelectScreen";
import { useAuthController } from "./useAuthController";
import { useAuthStyles } from "./useAuthStyles";
import { WelcomeScreen } from "./WelcomeScreen";

/**
 * Mobil oturum ekranı seçici: kontrol ediliyor → giriş → okul seç → hoş geldin (spec 0004); girişte "Şifremi unuttum",
 * hoş geldin'de "Hesabım" (spec 0006). Platform yöneticisi girişte "web'den giriş yapın" mesajı görür (spec 0007).
 * Hoş geldin'de "Okul takvimi" (spec 0008, her rol).
 */
export function AuthGate() {
  const { state, session, login, retryResume, selectSchool, switchSchool, logout, reloadMe, restart } = useAuthController();
  const styles = useAuthStyles();
  const [forgot, setForgot] = useState(false);
  const [account, setAccount] = useState(false);
  const [calendar, setCalendar] = useState(false);
  const [previousKind, setPreviousKind] = useState(state.kind);

  // Oturum durumundan çıkınca (çıkış, oturum düştü) Hesabım kapanır: sonraki girişte Hoş geldin açılır (review F-6).
  if (previousKind !== state.kind) {
    setPreviousKind(state.kind);
    if (state.kind !== "signedIn") {
      setAccount(false);
      setCalendar(false);
    }

    if (state.kind !== "signedOut") {
      setForgot(false);
    }
  }

  switch (state.kind) {
    case "restoring":
      return <Text style={styles.notice}>{t("auth.restoring")}</Text>;
    case "signedOut":
      return forgot ? (
        <ForgotPasswordScreen onBack={() => setForgot(false)} />
      ) : (
        <LoginScreen submitting={state.submitting} expired={state.expired} problem={state.problem} onSubmit={login} onRetryResume={retryResume} onForgot={() => setForgot(true)} />
      );
    case "twoFactor":
      // Platform yöneticisi (spec 0007): kod adımı ve panel yalnızca web'de.
      return (
        <View style={styles.card} testID="platform-web-only">
          <Text style={styles.text} accessibilityRole="alert">{t("auth.platformWebOnly")}</Text>
          <Pressable style={styles.secondary} onPress={restart} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("auth.backToLogin")}</Text>
          </Pressable>
        </View>
      );
    case "selectingSchool":
      return <SchoolSelectScreen tenants={state.tenants} selecting={state.selecting} failed={state.failed} onSelect={selectSchool} />;
    case "loadingMe":
      return <Text style={styles.notice}>{t("welcome.loading")}</Text>;
    case "signedIn":
      if (calendar) {
        return <CalendarScreen client={session.client} onBack={() => setCalendar(false)} />;
      }

      return account ? (
        <AccountScreen client={session.client} onBack={() => setAccount(false)} onProfileSaved={() => void reloadMe()} />
      ) : (
        <WelcomeScreen me={state.me} onSwitchSchool={switchSchool} onLogout={logout} onAccount={() => setAccount(true)} onCalendar={() => setCalendar(true)} />
      );
  }
}
