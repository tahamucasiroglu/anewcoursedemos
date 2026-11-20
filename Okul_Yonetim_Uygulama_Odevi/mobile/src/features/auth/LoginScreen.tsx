import { useState } from "react";
import { Pressable, Text, TextInput, View } from "react-native";
import { validateLoginForm, type LoginFormErrors, type LoginProblem } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useTheme } from "../../theme/useTheme";
import { useAuthStyles } from "./useAuthStyles";

interface Props {
  submitting: boolean;
  expired: boolean;
  problem: LoginProblem | null;
  onSubmit: (email: string, password: string) => void;
  onRetryResume: () => void;
}

/**
 * Mobil giriş ekranı (spec 0004): web ile aynı kurallar ve metinler. Alan hataları istek gönderilmeden gösterilir;
 * hatalar ekran okuyucuya duyurulur (accessibilityRole="alert" + canlı bölge). Gönderilirken düğme kilitli.
 */
export function LoginScreen({ submitting, expired, problem, onSubmit, onRetryResume }: Props) {
  const styles = useAuthStyles();
  const { colors } = useTheme();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errors, setErrors] = useState<LoginFormErrors>({});

  const submit = () => {
    const found = validateLoginForm(email, password);
    setErrors(found);
    if (!found.email && !found.password) {
      onSubmit(email.trim(), password);
    }
  };

  const message =
    problem?.kind === "server" ? problem.message : problem?.kind === "invalid" ? t("auth.invalidCredentials") : problem?.kind === "unreachable" ? t("auth.unreachable") : null;

  return (
    <View style={styles.card} testID="login">
      <Text style={styles.title} accessibilityRole="header">
        {t("auth.title")}
      </Text>
      {expired && <Text style={styles.notice}>{t("auth.expired")}</Text>}
      <Text style={styles.label}>{t("auth.email")}</Text>
      <TextInput
        style={[styles.input, errors.email && styles.inputInvalid]}
        accessibilityLabel={t("auth.email")}
        accessibilityHint={errors.email ? t(`auth.${errors.email}`) : undefined}
        value={email}
        onChangeText={setEmail}
        autoCapitalize="none"
        autoComplete="email"
        keyboardType="email-address"
        placeholderTextColor={colors.textMuted}
      />
      {errors.email && (
        <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
          {t(`auth.${errors.email}`)}
        </Text>
      )}
      <Text style={styles.label}>{t("auth.password")}</Text>
      <TextInput
        style={[styles.input, errors.password && styles.inputInvalid]}
        accessibilityLabel={t("auth.password")}
        accessibilityHint={errors.password ? t(`auth.${errors.password}`) : undefined}
        value={password}
        onChangeText={setPassword}
        secureTextEntry
        autoComplete="current-password"
        onSubmitEditing={submit}
      />
      {errors.password && (
        <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
          {t(`auth.${errors.password}`)}
        </Text>
      )}
      {message && (
        <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
          {message}
        </Text>
      )}
      <View style={styles.actions}>
        <Pressable
          style={[styles.button, submitting && styles.buttonDisabled]}
          onPress={submit}
          disabled={submitting}
          accessibilityRole="button"
          accessibilityState={{ disabled: submitting, busy: submitting }}
        >
          <Text style={styles.buttonText}>{submitting ? t("auth.submitting") : t("auth.submit")}</Text>
        </Pressable>
        {problem?.kind === "unreachable" && !submitting && (
          <Pressable style={styles.secondary} onPress={() => (problem.resume ? onRetryResume() : submit())} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("auth.retry")}</Text>
          </Pressable>
        )}
      </View>
    </View>
  );
}
