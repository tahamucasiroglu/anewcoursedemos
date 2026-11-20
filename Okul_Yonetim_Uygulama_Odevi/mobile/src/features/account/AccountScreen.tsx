import { useEffect, useState } from "react";
import { Pressable, Text, TextInput, View } from "react-native";
import {
  callApi,
  validatePasswordChange,
  validateProfileForm,
  type AccountFieldError,
  type AuthSession,
  type MyProfile,
  type PasswordChangeErrors,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";

type Client = AuthSession["client"];
const fieldText = (error: AccountFieldError) => t(`account.field.${error}` as TextKey);

interface Props {
  client: Client;
  onBack: () => void;
  onProfileSaved: () => void;
}

/** Hesabım (spec 0006, mobil): profil (ad-soyad, telefon; e-posta salt okunur) ve şifremi değiştir; web ile aynı kurallar. */
export function AccountScreen({ client, onBack, onProfileSaved }: Props) {
  const styles = useAuthStyles();
  const [profile, setProfile] = useState<MyProfile | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [reload, setReload] = useState(0);
  const [values, setValues] = useState({ fullName: "", phone: "" });
  const [profileErrors, setProfileErrors] = useState<ReturnType<typeof validateProfileForm>>({});
  const [profileResult, setProfileResult] = useState<{ error: boolean; text: string } | null>(null);
  const [savingProfile, setSavingProfile] = useState(false);
  const [password, setPassword] = useState({ current: "", next: "", repeat: "" });
  const [passwordErrors, setPasswordErrors] = useState<PasswordChangeErrors>({});
  const [passwordResult, setPasswordResult] = useState<{ error: boolean; text: string } | null>(null);
  const [changing, setChanging] = useState(false);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/account/profile")).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setProfile(outcome.data);
        setValues({ fullName: outcome.data.fullName, phone: outcome.data.phone ?? "" });
        setLoadError(null);
      } else {
        setLoadError(outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("account.loadError"));
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  const saveProfile = async () => {
    const found = validateProfileForm(values);
    setProfileErrors(found);
    setProfileResult(null);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSavingProfile(true);
    const outcome = await callApi(client.PUT("/api/account/profile", { body: { fullName: values.fullName.trim(), phone: values.phone.trim() || null } }));
    setSavingProfile(false);
    if (outcome.ok) {
      setValues({ fullName: outcome.data.fullName, phone: outcome.data.phone ?? "" });
      setProfileResult({ error: false, text: t("account.profileSaved") });
      onProfileSaved();
    } else {
      setProfileResult({ error: true, text: outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("account.loadError") });
    }
  };

  const changePassword = async () => {
    const found = validatePasswordChange(password);
    setPasswordErrors(found);
    setPasswordResult(null);
    if (Object.keys(found).length > 0) {
      return;
    }

    setChanging(true);
    const outcome = await callApi(client.POST("/api/account/change-password", {
      body: { currentPassword: password.current, newPassword: password.next, newPasswordRepeat: password.repeat },
    }));
    setChanging(false);
    if (outcome.ok) {
      setPassword({ current: "", next: "", repeat: "" });
      setPasswordResult({ error: false, text: t("account.passwordChanged") });
    } else {
      setPasswordResult({ error: true, text: outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("account.loadError") });
    }
  };

  const input = (label: string, value: string, onChange: (v: string) => void, error: AccountFieldError | undefined, secure = false, autoComplete?: "name" | "tel" | "current-password" | "new-password") => (
    <>
      <Text style={styles.label}>{label}</Text>
      <TextInput style={[styles.input, error && styles.inputInvalid]} accessibilityLabel={label} accessibilityHint={error ? fieldText(error) : undefined}
        value={value} onChangeText={onChange} secureTextEntry={secure} autoComplete={autoComplete} autoCapitalize={secure ? "none" : undefined} />
      {error && <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">{fieldText(error)}</Text>}
    </>
  );

  const result = (value: { error: boolean; text: string } | null) =>
    value && (
      <Text style={value.error ? styles.error : styles.notice} accessibilityRole={value.error ? "alert" : "text"} accessibilityLiveRegion="polite">
        {value.text}
      </Text>
    );

  return (
    <View style={styles.card} testID="account">
      <Text style={styles.title} accessibilityRole="header">{t("account.title")}</Text>
      {loadError && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{loadError}</Text>
          <Pressable style={styles.secondary} onPress={() => setReload((n) => n + 1)} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("admin.retry")}</Text>
          </Pressable>
        </>
      )}
      {!profile && !loadError && <Text style={styles.notice}>{t("admin.loading")}</Text>}
      {profile && (
        <>
          <Text style={styles.muted} accessibilityRole="header">{t("account.profile")}</Text>
          {input(t("members.fullName"), values.fullName, (v) => setValues({ ...values, fullName: v }), profileErrors.fullName, false, "name")}
          <Text style={styles.label}>{t("members.email")}</Text>
          <Text style={styles.text}>{profile.email}</Text>
          <Text style={styles.notice}>{t("account.emailReadonly")}</Text>
          {input(t("members.phone"), values.phone, (v) => setValues({ ...values, phone: v }), profileErrors.phone, false, "tel")}
          {result(profileResult)}
          <Pressable style={[styles.button, savingProfile && styles.buttonDisabled]} onPress={saveProfile} disabled={savingProfile} accessibilityRole="button" accessibilityState={{ disabled: savingProfile, busy: savingProfile }}>
            <Text style={styles.buttonText}>{savingProfile ? t("members.saving") : t("members.save")}</Text>
          </Pressable>
        </>
      )}
      <Text style={styles.muted} accessibilityRole="header">{t("account.changePassword")}</Text>
      {input(t("account.currentPassword"), password.current, (v) => setPassword({ ...password, current: v }), passwordErrors.current, true, "current-password")}
      {input(t("account.newPassword"), password.next, (v) => setPassword({ ...password, next: v }), passwordErrors.next, true, "new-password")}
      {input(t("account.newPasswordRepeat"), password.repeat, (v) => setPassword({ ...password, repeat: v }), passwordErrors.repeat, true, "new-password")}
      {result(passwordResult)}
      <View style={styles.actions}>
        <Pressable style={[styles.button, changing && styles.buttonDisabled]} onPress={changePassword} disabled={changing} accessibilityRole="button" accessibilityState={{ disabled: changing, busy: changing }}>
          <Text style={styles.buttonText}>{changing ? t("account.changing") : t("account.changeSubmit")}</Text>
        </Pressable>
        <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
          <Text style={styles.secondaryText}>{t("account.back")}</Text>
        </Pressable>
      </View>
    </View>
  );
}
