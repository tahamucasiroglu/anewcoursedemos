import { useState } from "react";
import { Pressable, Text, View } from "react-native";
import { t } from "../../i18n/t";
import { AccountScreen } from "../account/AccountScreen";
import { MyAppointmentsScreen } from "../appointments/MyAppointmentsScreen";
import { TeacherScheduleScreen } from "../appointments/TeacherScheduleScreen";
import { ChildAcademicsScreen } from "../academics/ChildAcademicsScreen";
import { TeacherAcademicsScreen } from "../academics/TeacherAcademicsScreen";
import { CalendarScreen } from "../calendar/CalendarScreen";
import { ChildCardScreen } from "../children/ChildCardScreen";
import { ChildrenScreen } from "../children/ChildrenScreen";
import { MyClassroomsScreen } from "../attendance/MyClassroomsScreen";
import { NoticesScreen } from "../notices/NoticesScreen";
import { PaymentsScreen } from "../payments/PaymentsScreen";
import { ConversationsScreen } from "../communication/ConversationsScreen";
import { FeedScreen } from "../communication/FeedScreen";
import { QuietHoursScreen } from "../communication/QuietHoursScreen";
import { ForgotPasswordScreen } from "./ForgotPasswordScreen";
import { LoginScreen } from "./LoginScreen";
import { SchoolSelectScreen } from "./SchoolSelectScreen";
import { useAuthController } from "./useAuthController";
import { useAuthStyles } from "./useAuthStyles";
import { WelcomeScreen } from "./WelcomeScreen";

/**
 * Mobil oturum ekranı seçici: kontrol ediliyor → giriş → okul seç → hoş geldin (spec 0004); girişte "Şifremi unuttum",
 * hoş geldin'de "Hesabım" (spec 0006). Platform yöneticisi girişte "web'den giriş yapın" mesajı görür (spec 0007).
 * Hoş geldin'de "Okul takvimi" (spec 0008, her rol) ve yalnızca velide "Çocuklarım" → çocuk kartı (spec 0009). Öğretmende "Yoklama al" → Sınıflarım/yoklama, velide
 * "Bildirimler (n)"; bildirimlerden dönünce okunmamış sayısı için kişi bilgisi yenilenir (spec 0010). Velide "Duyurular (n)",
 * veli ve öğretmende "Mesajlar (n)", öğretmende "Mesai saatlerim" (spec 0011); dönünce sayılar için kişi bilgisi yenilenir.
 * Spec 0013: öğretmende "Ödev ve not", velide "Ödev ve notlar" (ayrıca çocuk kartından o çocuğun özeti; dönüş karta). Spec 0015: velide
 * "Ödemeler" (salt okunur). Spec 0016: öğretmende "Randevular (n bekleyen)" → Takvimim, velide "Randevular" → Randevularım; dönünce
 * bekleyen sayısı için kişi bilgisi yenilenir.
 */
export function AuthGate() {
  const { state, session, login, retryResume, selectSchool, switchSchool, logout, reloadMe, restart } = useAuthController();
  const styles = useAuthStyles();
  const [forgot, setForgot] = useState(false);
  const [account, setAccount] = useState(false);
  const [calendar, setCalendar] = useState(false);
  const [children, setChildren] = useState<{ childId: string | null } | null>(null);
  const [attendance, setAttendance] = useState(false);
  const [notices, setNotices] = useState(false);
  const [communication, setCommunication] = useState<"feed" | "messages" | "quietHours" | null>(null);
  const [academics, setAcademics] = useState(false);
  const [childAcademics, setChildAcademics] = useState<{ childId?: string } | null>(null);
  const [payments, setPayments] = useState(false);
  const [appointments, setAppointments] = useState<"teacher" | "guardian" | null>(null);
  const [previousKind, setPreviousKind] = useState(state.kind);

  // Oturum durumundan çıkınca (çıkış, oturum düştü) Hesabım kapanır: sonraki girişte Hoş geldin açılır (review F-6).
  if (previousKind !== state.kind) {
    setPreviousKind(state.kind);
    if (state.kind !== "signedIn") {
      setAccount(false);
      setCalendar(false);
      setChildren(null);
      setAttendance(false);
      setNotices(false);
      setCommunication(null);
      setAcademics(false);
      setChildAcademics(null);
      setPayments(false);
      setAppointments(null);
    }

    if (state.kind !== "signedOut") {
      setForgot(false);
    }
  }

  switch (state.kind) {
    case "restoring":
      return <Text style={styles.notice}>{t("auth.restoring")}</Text>;
    case "signedOut":
      return forgot ? (
        <ForgotPasswordScreen onBack={() => setForgot(false)} />
      ) : (
        <LoginScreen submitting={state.submitting} expired={state.expired} problem={state.problem} onSubmit={login} onRetryResume={retryResume} onForgot={() => setForgot(true)} />
      );
    case "twoFactor":
      // Platform yöneticisi (spec 0007): kod adımı ve panel yalnızca web'de.
      return (
        <View style={styles.card} testID="platform-web-only">
          <Text style={styles.text} accessibilityRole="alert">{t("auth.platformWebOnly")}</Text>
          <Pressable style={styles.secondary} onPress={restart} accessibilityRole="button">
            <Text style={styles.secondaryText}>{t("auth.backToLogin")}</Text>
          </Pressable>
        </View>
      );
    case "selectingSchool":
      return <SchoolSelectScreen tenants={state.tenants} selecting={state.selecting} failed={state.failed} onSelect={selectSchool} />;
    case "loadingMe":
      return <Text style={styles.notice}>{t("welcome.loading")}</Text>;
    case "signedIn":
      if (academics) {
        return <TeacherAcademicsScreen client={session.client} onBack={() => setAcademics(false)} />;
      }

      if (childAcademics) {
        return <ChildAcademicsScreen key={childAcademics.childId ?? "all"} client={session.client} childId={childAcademics.childId} onBack={() => setChildAcademics(null)} />;
      }

      if (payments) {
        return <PaymentsScreen client={session.client} onBack={() => setPayments(false)} />;
      }

      if (appointments) {
        const back = () => {
          setAppointments(null);
          void reloadMe();
        };
        return appointments === "teacher" ? (
          <TeacherScheduleScreen client={session.client} onBack={back} />
        ) : (
          <MyAppointmentsScreen client={session.client} onBack={back} />
        );
      }

      if (attendance) {
        return <MyClassroomsScreen client={session.client} onBack={() => setAttendance(false)} />;
      }

      if (notices) {
        return (
          <NoticesScreen
            client={session.client}
            onBack={() => {
              setNotices(false);
              void reloadMe();
            }}
          />
        );
      }

      if (communication) {
        const back = () => {
          setCommunication(null);
          void reloadMe();
        };
        switch (communication) {
          case "feed":
            return <FeedScreen client={session.client} onBack={back} />;
          case "messages":
            return <ConversationsScreen client={session.client} onBack={back} />;
          default:
            return <QuietHoursScreen client={session.client} onBack={back} />;
        }
      }

      if (children) {
        return children.childId ? (
          <ChildCardScreen key={children.childId} client={session.client} childId={children.childId} onBack={() => setChildren({ childId: null })} onAcademics={() => setChildAcademics({ childId: children.childId ?? undefined })} />
        ) : (
          <ChildrenScreen client={session.client} onOpen={(childId) => setChildren({ childId })} onBack={() => setChildren(null)} />
        );
      }

      if (calendar) {
        return <CalendarScreen client={session.client} onBack={() => setCalendar(false)} />;
      }

      return account ? (
        <AccountScreen client={session.client} onBack={() => setAccount(false)} onProfileSaved={() => void reloadMe()} />
      ) : (
        <WelcomeScreen me={state.me} onSwitchSchool={switchSchool} onLogout={logout} onAccount={() => setAccount(true)} onCalendar={() => setCalendar(true)} onChildren={() => setChildren({ childId: null })} onAttendance={() => setAttendance(true)} onNotices={() => setNotices(true)} onFeed={() => setCommunication("feed")} onMessages={() => setCommunication("messages")} onQuietHours={() => setCommunication("quietHours")} onAcademics={() => setAcademics(true)} onChildAcademics={() => setChildAcademics({})} onPayments={() => setPayments(true)} onTeacherAppointments={() => setAppointments("teacher")} onGuardianAppointments={() => setAppointments("guardian")} />
      );
  }
}
