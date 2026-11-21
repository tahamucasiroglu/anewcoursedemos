import { Pressable, Text, View } from "react-native";
import { countedText, noticesLinkText, type Me } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "./useAuthStyles";

interface Props {
  me: Me;
  onSwitchSchool: () => void;
  onLogout: () => void;
  onAccount?: () => void;
  onCalendar?: () => void;
  onChildren?: () => void;
  onAttendance?: () => void;
  onNotices?: () => void;
  onFeed?: () => void;
  onMessages?: () => void;
  onQuietHours?: () => void;
  onAcademics?: () => void;
  onChildAcademics?: () => void;
  onPayments?: () => void;
}

const ROLE_KEYS: Record<string, TextKey> = {
  Founder: "role.Founder",
  Teacher: "role.Teacher",
  Guardian: "role.Guardian",
  Accounting: "role.Accounting",
};

/**
 * Mobil hoş geldin ekranı (spec 0004): ad, okul, roller, şubeler (yoksa açık mesaj); okul değiştir ve çıkış. Seçili okulu
 * olan herkese "Okul takvimi" (spec 0008); yalnızca veli rolü olana "Çocuklarım" (spec 0009) ve okunmamış sayılı "Bildirimler (n)", yalnızca öğretmene
 * "Yoklama al" (spec 0010). Spec 0011: velide okunmamış sayılı "Duyurular (n)", veli ve öğretmende "Mesajlar (n)", öğretmende
 * "Mesai saatlerim". Spec 0013: öğretmende "Ödev ve not", velide "Ödev ve notlar". Spec 0015: yalnızca velide "Ödemeler".
 */
export function WelcomeScreen({ me, onSwitchSchool, onLogout, onAccount, onCalendar, onChildren, onAttendance, onNotices, onFeed, onMessages, onQuietHours, onAcademics, onChildAcademics, onPayments }: Props) {
  const isTeacher = me.roles.includes("Teacher");
  const isGuardian = me.roles.includes("Guardian");
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
        {onAttendance && me.roles.includes("Teacher") && (
          <Pressable style={styles.button} onPress={onAttendance} accessibilityRole="button">
            <Text style={styles.buttonText}>{t("attendance.link")}</Text>
          </Pressable>
        )}
        {onAcademics && isTeacher && (
          <Pressable style={styles.secondary} onPress={onAcademics} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("academics.link")}</Text>
          </Pressable>
        )}
        {onChildAcademics && isGuardian && (
          <Pressable style={styles.secondary} onPress={onChildAcademics} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("childAcademics.link")}</Text>
          </Pressable>
        )}
        {onPayments && isGuardian && (
          <Pressable style={styles.secondary} onPress={onPayments} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("payments.link")}</Text>
          </Pressable>
        )}
        {onNotices && me.roles.includes("Guardian") && (
          <Pressable style={styles.secondary} onPress={onNotices} accessibilityRole="button">
            <Text style={styles.secondaryText}>{noticesLinkText(me.unreadNoticeCount, t("notices.link"), t("notices.linkCount"))}</Text>
          </Pressable>
        )}
        {onFeed && isGuardian && (
          <Pressable
            style={styles.secondary}
            onPress={onFeed}
            accessibilityRole="button"
            accessibilityLabel={countedText(me.unreadAnnouncementCount, t("feed.link"), t("feed.linkLabel"))}
          >
            <Text style={styles.secondaryText}>{countedText(me.unreadAnnouncementCount, t("feed.link"), t("feed.linkCount"))}</Text>
          </Pressable>
        )}
        {onMessages && (isGuardian || isTeacher) && (
          <Pressable
            style={styles.secondary}
            onPress={onMessages}
            accessibilityRole="button"
            accessibilityLabel={countedText(me.unreadMessageCount, t("messages.link"), t("messages.linkLabel"))}
          >
            <Text style={styles.secondaryText}>{countedText(me.unreadMessageCount, t("messages.link"), t("messages.linkCount"))}</Text>
          </Pressable>
        )}
        {onQuietHours && isTeacher && (
          <Pressable style={styles.secondary} onPress={onQuietHours} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("quiet.link")}</Text>
          </Pressable>
        )}
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
