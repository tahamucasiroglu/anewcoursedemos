import { Text } from "react-native";
import { t } from "../../i18n/t";
import { LoginScreen } from "./LoginScreen";
import { SchoolSelectScreen } from "./SchoolSelectScreen";
import { useAuthController } from "./useAuthController";
import { useAuthStyles } from "./useAuthStyles";
import { WelcomeScreen } from "./WelcomeScreen";

/** Mobil oturum ekranı seçici: kontrol ediliyor → giriş → okul seç → hoş geldin (spec 0004). */
export function AuthGate() {
  const { state, login, selectSchool, switchSchool, logout } = useAuthController();
  const styles = useAuthStyles();

  switch (state.kind) {
    case "restoring":
      return <Text style={styles.notice}>{t("auth.restoring")}</Text>;
    case "signedOut":
      return <LoginScreen submitting={state.submitting} expired={state.expired} problem={state.problem} onSubmit={login} />;
    case "selectingSchool":
      return <SchoolSelectScreen tenants={state.tenants} selecting={state.selecting} failed={state.failed} onSelect={selectSchool} />;
    case "loadingMe":
      return <Text style={styles.notice}>{t("welcome.loading")}</Text>;
    case "signedIn":
      return <WelcomeScreen me={state.me} onSwitchSchool={switchSchool} onLogout={logout} />;
  }
}
