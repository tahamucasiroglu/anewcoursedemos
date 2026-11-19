import { useMemo, type ReactNode } from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { t } from "../../i18n/t";
import { ThemeSwitcher } from "../../theme/ThemeSwitcher";
import { useTheme } from "../../theme/useTheme";
import { useSystemStatus } from "./useSystemStatus";

/**
 * Mobil sistem durumu ekranı (spec 0001): yükleniyor / çalışıyor / hizmet dışı / sunucuya ulaşılamıyor; son ikisinde
 * "Tekrar dene". Durum ekran okuyucuya duyurulur (accessibilityLiveRegion) ve yalnızca renkle değil metinle anlatılır.
 * Renkler ortak temadan; başlık satırında tema seçici var, dar ekranda alt satıra iner (spec 0002). Altında oturum
 * alanı (giriş / okul seç / hoş geldin — spec 0004) çocuk olarak gelir.
 */
export function SystemStatusScreen({ children }: { children?: ReactNode }) {
  const { state, retry } = useSystemStatus();
  const { colors } = useTheme();
  const styles = useMemo(
    () =>
      StyleSheet.create({
        container: { flex: 1, justifyContent: "center", padding: 24, backgroundColor: colors.background },
        header: { flexDirection: "row", flexWrap: "wrap", alignItems: "center", justifyContent: "space-between", gap: 12, marginBottom: 16 },
        title: { fontSize: 24, fontWeight: "600", color: colors.text },
        card: { padding: 16, borderRadius: 8, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface, gap: 12 },
        cardHealthy: { borderColor: colors.success },
        cardError: { borderColor: colors.danger },
        text: { fontSize: 16, color: colors.text },
        button: { alignSelf: "flex-start", paddingVertical: 8, paddingHorizontal: 16, borderRadius: 6, backgroundColor: colors.primary },
        buttonText: { color: colors.onPrimary, fontSize: 16 },
      }),
    [colors],
  );

  const text =
    state.kind === "loading"
      ? t("status.loading")
      : state.kind === "unreachable"
        ? t("status.unreachable")
        : state.message;
  const canRetry = state.kind === "unavailable" || state.kind === "unreachable";

  return (
    <View style={styles.container} testID="screen">
      <View style={styles.header}>
        <Text style={styles.title} accessibilityRole="header">
          {t("app.title")}
        </Text>
        <ThemeSwitcher />
      </View>
      <View
        style={[styles.card, state.kind === "healthy" && styles.cardHealthy, canRetry && styles.cardError]}
        accessibilityLiveRegion="polite"
        accessibilityRole="summary"
        testID="system-status"
      >
        <Text style={styles.text}>
          {state.kind === "healthy" ? "✔ " : ""}
          {text}
        </Text>
        {canRetry && (
          <Pressable style={styles.button} onPress={retry} accessibilityRole="button">
            <Text style={styles.buttonText}>{t("status.retry")}</Text>
          </Pressable>
        )}
      </View>
      {children}
    </View>
  );
}
