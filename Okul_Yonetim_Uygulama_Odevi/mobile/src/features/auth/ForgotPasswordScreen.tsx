import { useState } from "react";
import { Pressable, Text, TextInput, View } from "react-native";
import { callApi, createOkulApiClient, validateEmailField, type AccountFieldError } from "@okul/api-client";
import { apiBaseUrl } from "../../config";
import { t, type TextKey } from "../../i18n/t";
import { useTheme } from "../../theme/useTheme";
import { useAuthStyles } from "./useAuthStyles";

/** Şifremi unuttum (spec 0006, mobil): e-posta → her durumda aynı mesaj; bağlantı e-postayla gelir ve web'de açılır. */
export function ForgotPasswordScreen({ onBack }: { onBack: () => void }) {
  const styles = useAuthStyles();
  const { colors } = useTheme();
  const [client] = useState(() => createOkulApiClient(apiBaseUrl));
  const [email, setEmail] = useState("");
  const [error, setError] = useState<AccountFieldError | null>(null);
  const [sending, setSending] = useState(false);
  const [sent, setSent] = useState<string | null>(null);
  const [failed, setFailed] = useState<string | null>(null);

  const submit = async () => {
    const found = validateEmailField(email);
    setError(found);
    setFailed(null);
    if (found) {
      return;
    }

    setSending(true);
    const outcome = await callApi(client.POST("/api/auth/forgot-password", { body: { email: email.trim() } }));
    setSending(false);
    if (outcome.ok) {
      setSent(outcome.data.message);
    } else {
      setFailed(outcome.unreachable ? t("auth.unreachable") : outcome.message ?? t("account.loadError"));
    }
  };

  return (
    <View style={styles.card} testID="forgot">
      <Text style={styles.title} accessibilityRole="header">{t("account.forgotTitle")}</Text>
      {sent ? (
        <Text style={styles.text} accessibilityLiveRegion="polite">{sent}</Text>
      ) : (
        <>
          <Text style={styles.notice}>{t("account.forgotIntro")}</Text>
          <Text style={styles.label}>{t("auth.email")}</Text>
          <TextInput style={[styles.input, error && styles.inputInvalid]} accessibilityLabel={t("auth.email")} accessibilityHint={error ? t(`account.field.${error}` as TextKey) : undefined}
            value={email} onChangeText={setEmail} autoCapitalize="none" autoComplete="email" keyboardType="email-address" placeholderTextColor={colors.textMuted} onSubmitEditing={submit} />
          {error && <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">{t(`account.field.${error}` as TextKey)}</Text>}
          {failed && <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">{failed}</Text>}
        </>
      )}
      <View style={styles.actions}>
        {!sent && (
          <Pressable style={[styles.button, sending && styles.buttonDisabled]} onPress={submit} disabled={sending} accessibilityRole="button" accessibilityState={{ disabled: sending, busy: sending }}>
            <Text style={styles.buttonText}>{sending ? t("account.sending") : failed ? t("auth.retry") : t("account.forgotSubmit")}</Text>
          </Pressable>
        )}
        <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
          <Text style={styles.secondaryText}>{t("account.backToLogin")}</Text>
        </Pressable>
      </View>
    </View>
  );
}
