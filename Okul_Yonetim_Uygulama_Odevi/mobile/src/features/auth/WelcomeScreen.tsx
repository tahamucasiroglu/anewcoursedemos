import { Pressable, Text, View } from "react-native";
import type { Me } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "./useAuthStyles";

interface Props {
  me: Me;
  onSwitchSchool: () => void;
  onLogout: () => void;
  onAccount?: () => void;
  onCalendar?: () => void;
  onChildren?: () => void;
}

const ROLE_KEYS: Record<string, TextKey> = {
  Founder: "role.Founder",
  Teacher: "role.Teacher",
  Guardian: "role.Guardian",
  Accounting: "role.Accounting",
};

/**
 * Mobil hoş geldin ekranı (spec 0004): ad, okul, roller, şubeler (yoksa açık mesaj); okul değiştir ve çıkış. Seçili okulu
 * olan herkese "Okul takvimi" (spec 0008); yalnızca veli rolü olana "Çocuklarım" (spec 0009).
 */
export function WelcomeScreen({ me, onSwitchSchool, onLogout, onAccount, onCalendar, onChildren }: Props) {
  const styles = useAuthStyles();
  return (
    <View style={styles.card} testID="welcome">
      <Text style={styles.title} accessibilityRole="header">
        {t("welcome.greeting")} {me.fullName}
      </Text>
      <Text style={styles.muted}>{t("welcome.school")}</Text>
      <Text style={styles.text}>{me.tenant?.name}</Text>
      <Text style={styles.muted}>{t("welcome.roles")}</Text>
      <Text style={styles.text}>{me.roles.map((role) => (ROLE_KEYS[role] ? t(ROLE_KEYS[role]) : role)).join(", ")}</Text>
      <Text style={styles.muted}>{t("welcome.branches")}</Text>
      {me.branches.length === 0 ? (
        <Text style={styles.text}>{t("welcome.noBranches")}</Text>
      ) : (
        me.branches.map((branch) => (
          <Text key={branch.id} style={styles.text}>
            {branch.name}
          </Text>
        ))
      )}
      <View style={styles.actions}>
        {onChildren && me.roles.includes("Guardian") && (
          <Pressable style={styles.secondary} onPress={onChildren} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("children.link")}</Text>
          </Pressable>
        )}
        {onCalendar && me.tenant && (
          <Pressable style={styles.secondary} onPress={onCalendar} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("calendar.link")}</Text>
          </Pressable>
        )}
        {onAccount && (
          <Pressable style={styles.secondary} onPress={onAccount} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("account.link")}</Text>
          </Pressable>
        )}
        {me.tenants.length > 1 && (
          <Pressable style={styles.secondary} onPress={onSwitchSchool} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("welcome.switchSchool")}</Text>
          </Pressable>
        )}
        <Pressable style={styles.button} onPress={onLogout} accessibilityRole="button">
          <Text style={styles.buttonText}>{t("welcome.logout")}</Text>
        </Pressable>
      </View>
    </View>
  );
}
