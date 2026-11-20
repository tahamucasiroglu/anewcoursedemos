import { useMemo } from "react";
import { Pressable, StyleSheet, Text, View } from "react-native";
import { ATTENDANCE_STATUSES, type AttendanceStatus } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useTheme } from "../../theme/useTheme";

interface Props {
  fullName: string;
  status: AttendanceStatus;
  disabled: boolean;
  onChange: (status: AttendanceStatus) => void;
}

/**
 * Yoklama listesindeki öğrenci satırı (spec 0010): ad ve üç seçim (Var / Yok / Geç). Bir dokunuş durumu değiştirir. Her
 * düğmenin etiketi öğrenci adını içerir ("Ayşe Yılmaz: Yok"), seçili olan ekran okuyucuya bildirilir (radio + checked) ve
 * yazıyla da belirtilir (✓); dokunma alanı en az 44×44; renkler yalnızca ortak temadan.
 */
export function AttendanceRow({ fullName, status, disabled, onChange }: Props) {
  const { colors } = useTheme();
  const styles = useMemo(
    () =>
      StyleSheet.create({
        row: { gap: 6, paddingVertical: 6, borderTopWidth: 1, borderColor: colors.border },
        name: { fontSize: 16, color: colors.text },
        choices: { flexDirection: "row", gap: 8 },
        choice: { minWidth: 64, minHeight: 44, justifyContent: "center", alignItems: "center", paddingHorizontal: 10, borderRadius: 6, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
        chosen: { backgroundColor: colors.primary, borderColor: colors.primary },
        choiceText: { fontSize: 16, color: colors.text },
        chosenText: { fontSize: 16, fontWeight: "600", color: colors.onPrimary },
      }),
    [colors],
  );

  return (
    <View style={styles.row} testID="attendance-row" accessibilityRole="radiogroup" accessibilityLabel={fullName}>
      <Text style={styles.name}>{fullName}</Text>
      <View style={styles.choices}>
        {ATTENDANCE_STATUSES.map((option) => {
          const label = t(`attendance.status.${option}` as TextKey);
          const selected = option === status;
          return (
            <Pressable
              key={option}
              style={[styles.choice, selected && styles.chosen]}
              accessibilityRole="radio"
              accessibilityLabel={`${fullName}: ${label}`}
              accessibilityState={{ checked: selected, disabled }}
              disabled={disabled}
              onPress={() => onChange(option)}
            >
              <Text style={selected ? styles.chosenText : styles.choiceText}>{selected ? `✓ ${label}` : label}</Text>
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}
