import { useMemo } from "react";
import { StyleSheet } from "react-native";
import { useTheme } from "../../theme/useTheme";

/**
 * Veli iletişimi ekranlarının (spec 0011) kendi stilleri — ortak kart/düğme stilleri `useAuthStyles`'tan gelir. Renkler
 * yalnızca ortak temadan (spec 0002): "yeni" işareti ve benim mesajım birincil renk + üstündeki yazı rengi, seçili gün ve
 * seçili onay düğmesi aynı çift; renk tek başına anlam taşımaz (metin ve erişilebilirlik etiketi de var).
 */
export function useCommunicationStyles() {
  const { colors } = useTheme();
  return useMemo(
    () =>
      StyleSheet.create({
        row: { gap: 4, paddingVertical: 8, paddingHorizontal: 12, borderRadius: 6, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
        badge: { alignSelf: "flex-start", paddingHorizontal: 8, paddingVertical: 2, borderRadius: 10, backgroundColor: colors.primary, color: colors.onPrimary, fontSize: 13, fontWeight: "600" },
        mine: { alignSelf: "flex-end", maxWidth: "85%", padding: 10, borderRadius: 8, backgroundColor: colors.primary },
        mineText: { color: colors.onPrimary, fontSize: 16 },
        theirs: { alignSelf: "flex-start", maxWidth: "85%", padding: 10, borderRadius: 8, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.background },
        theirsText: { color: colors.text, fontSize: 16 },
        choices: { flexDirection: "row", flexWrap: "wrap", gap: 8 },
        choice: { minWidth: 48, minHeight: 44, justifyContent: "center", alignItems: "center", paddingHorizontal: 10, borderRadius: 6, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
        chosen: { backgroundColor: colors.primary, borderColor: colors.primary },
        choiceText: { fontSize: 16, color: colors.text },
        chosenText: { fontSize: 16, fontWeight: "600", color: colors.onPrimary },
        multiline: { minHeight: 64, textAlignVertical: "top" },
      }),
    [colors],
  );
}
