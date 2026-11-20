import { useMemo } from "react";
import { StyleSheet } from "react-native";
import { useTheme } from "../../theme/useTheme";

/**
 * Giriş / okul seç / hoş geldin ekranlarının ortak stilleri; renkler yalnızca ortak temadan (spec 0002). Uzunluğu sınırsız
 * liste gösteren ekranın kartı `scrollCard` ile ekrana sığacak kadar küçülür, listesi `scrollList` (FlatList/ScrollView)
 * içinde kayar; başlık ve alttaki düğmeler sabit kalır (0010 review F-2, conventions T16).
 */
export function useAuthStyles() {
  const { colors } = useTheme();
  return useMemo(
    () =>
      StyleSheet.create({
        card: { marginTop: 16, padding: 16, borderRadius: 8, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface, gap: 10 },
        scrollCard: { flexShrink: 1, minHeight: 0 },
        scrollList: { flexGrow: 0, flexShrink: 1 },
        scrollContent: { gap: 10 },
        title: { fontSize: 20, fontWeight: "600", color: colors.text },
        label: { fontSize: 15, fontWeight: "600", color: colors.text },
        input: { borderWidth: 1, borderColor: colors.border, borderRadius: 6, paddingVertical: 8, paddingHorizontal: 12, fontSize: 16, color: colors.text, backgroundColor: colors.background },
        inputInvalid: { borderColor: colors.danger },
        error: { fontSize: 15, color: colors.danger },
        notice: { fontSize: 15, color: colors.textMuted },
        text: { fontSize: 16, color: colors.text },
        muted: { fontSize: 14, fontWeight: "600", color: colors.textMuted },
        actions: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
        button: { alignSelf: "flex-start", paddingVertical: 10, paddingHorizontal: 16, borderRadius: 6, backgroundColor: colors.primary },
        buttonDisabled: { opacity: 0.7 },
        buttonText: { color: colors.onPrimary, fontSize: 16 },
        secondary: { alignSelf: "stretch", paddingVertical: 10, paddingHorizontal: 16, borderRadius: 6, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
        secondaryText: { color: colors.text, fontSize: 16 },
        link: { color: colors.primary, fontSize: 16, textDecorationLine: "underline", paddingVertical: 8 },
      }),
    [colors],
  );
}
