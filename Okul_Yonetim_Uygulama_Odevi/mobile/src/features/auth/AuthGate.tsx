import { useState } from "react";
import { Text } from "react-native";
import { t } from "../../i18n/t";
import { AccountScreen } from "../account/AccountScreen";
import { ForgotPasswordScreen } from "./ForgotPasswordScreen";
import { LoginScreen } from "./LoginScreen";
import { SchoolSelectScreen } from "./SchoolSelectScreen";
import { useAuthController } from "./useAuthController";
import { useAuthStyles } from "./useAuthStyles";
import { WelcomeScreen } from "./WelcomeScreen";

/**
 * Mobil oturum ekranı seçici: kontrol ediliyor → giriş → okul seç → hoş geldin (spec 0004); girişte "Şifremi unuttum",
 * hoş geldin'de "Hesabım" (spec 0006).
 */
export function AuthGate() {
  const { state, session, login, retryResume, selectSchool, switchSchool, logout, reloadMe } = useAuthController();
  const styles = useAuthStyles();
  const [forgot, setForgot] = useState(false);
  const [account, setAccount] = useState(false);

  switch (state.kind) {
    case "restoring":
      return <Text style={styles.notice}>{t("auth.restoring")}</Text>;
    case "signedOut":
      return forgot ? (
        <ForgotPasswordScreen onBack={() => setForgot(false)} />
      ) : (
        <LoginScreen submitting={state.submitting} expired={state.expired} problem={state.problem} onSubmit={login} onRetryResume={retryResume} onForgot={() => setForgot(true)} />
      );
    case "selectingSchool":
      return <SchoolSelectScreen tenants={state.tenants} selecting={state.selecting} failed={state.failed} onSelect={selectSchool} />;
    case "loadingMe":
      return <Text style={styles.notice}>{t("welcome.loading")}</Text>;
    case "signedIn":
      return account ? (
        <AccountScreen client={session.client} onBack={() => setAccount(false)} onProfileSaved={() => void reloadMe()} />
      ) : (
        <WelcomeScreen me={state.me} onSwitchSchool={switchSchool} onLogout={logout} onAccount={() => setAccount(true)} />
      );
  }
}
