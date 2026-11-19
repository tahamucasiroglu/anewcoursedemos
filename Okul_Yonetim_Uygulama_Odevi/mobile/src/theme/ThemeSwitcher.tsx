import type { ThemePreference } from "@okul/theme";
import { useMemo } from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { t, type TextKey } from "../i18n/t";
import { useTheme } from "./useTheme";

/**
 * Mobil tema seçici (spec 0002): "Sistem · Açık · Koyu". Ekran okuyucu grubu "Tema" adıyla okur; her seçenek
 * radyo rolünde ve seçili olan "seçili" (checked) olarak bildirilir (AC-3, AC-11).
 */
const options: ReadonlyArray<{ value: ThemePreference; label: TextKey; icon: string }> = [
  { value: "system", label: "theme.system", icon: "◐" },
  { value: "light", label: "theme.light", icon: "☀" },
  { value: "dark", label: "theme.dark", icon: "☾" },
];

export function ThemeSwitcher() {
  const { preference, colors, setPreference } = useTheme();
  const styles = useMemo(
    () =>
      StyleSheet.create({
        group: { flexDirection: "row", borderWidth: 1, borderColor: colors.border, borderRadius: 8, overflow: "hidden" },
        option: { paddingVertical: 6, paddingHorizontal: 10, backgroundColor: colors.surface },
        divider: { borderLeftWidth: 1, borderLeftColor: colors.border },
        selected: { backgroundColor: colors.primary },
        label: { fontSize: 14, color: colors.text },
        selectedLabel: { color: colors.onPrimary, fontWeight: "600" },
      }),
    [colors],
  );

  return (
    <View style={styles.group} accessibilityRole="radiogroup" accessibilityLabel={t("theme.label")}>
      {options.map((option, index) => {
        const selected = preference === option.value;
        return (
          <Pressable
            key={option.value}
            style={[styles.option, index > 0 && styles.divider, selected && styles.selected]}
            onPress={() => setPreference(option.value)}
            accessibilityRole="radio"
            accessibilityLabel={t(option.label)}
            accessibilityState={{ checked: selected }}
          >
            <Text style={[styles.label, selected && styles.selectedLabel]}>
              {option.icon} {t(option.label)}
            </Text>
          </Pressable>
        );
      })}
    </View>
  );
}
