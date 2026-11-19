import { Pressable, StyleSheet, Text, View } from "react-native";
import { t } from "../../i18n/t";
import { useSystemStatus } from "./useSystemStatus";

/**
 * Mobil sistem durumu ekranı (spec 0001): yükleniyor / çalışıyor / hizmet dışı / sunucuya ulaşılamıyor; son ikisinde
 * "Tekrar dene". Durum ekran okuyucuya duyurulur (accessibilityLiveRegion) ve yalnızca renkle değil metinle anlatılır.
 */
export function SystemStatusScreen() {
  const { state, retry } = useSystemStatus();

  const text =
    state.kind === "loading"
      ? t("status.loading")
      : state.kind === "unreachable"
        ? t("status.unreachable")
        : state.message;
  const canRetry = state.kind === "unavailable" || state.kind === "unreachable";

  return (
    <View style={styles.container}>
      <Text style={styles.title} accessibilityRole="header">
        {t("app.title")}
      </Text>
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
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: "center", padding: 24, backgroundColor: "#f8fafc" },
  title: { fontSize: 24, fontWeight: "600", marginBottom: 16, color: "#1f2937" },
  card: { padding: 16, borderRadius: 8, borderWidth: 1, borderColor: "#cbd5e1", backgroundColor: "#ffffff", gap: 12 },
  cardHealthy: { borderColor: "#15803d" },
  cardError: { borderColor: "#b91c1c" },
  text: { fontSize: 16, color: "#1f2937" },
  button: { alignSelf: "flex-start", paddingVertical: 8, paddingHorizontal: 16, borderRadius: 6, backgroundColor: "#1e3a8a" },
  buttonText: { color: "#ffffff", fontSize: 16 },
});
