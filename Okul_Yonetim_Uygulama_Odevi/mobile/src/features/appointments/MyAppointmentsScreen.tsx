import { useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { callApi, fillText, type AuthSession, type MyAppointmentRow } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useCommunicationStyles } from "../communication/useCommunicationStyles";
import { BookAppointmentScreen } from "./BookAppointmentScreen";
import { failureText, useMyAppointments, whenText } from "./useAppointments";

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

/**
 * "Randevularım" (spec 0016 gereksinim 8; AC-10, AC-13 — veli): yükleniyor / hata + tekrar dene / boş ("Henüz randevunuz yok") / dolu:
 * yaklaşan (en yakın üstte) ve son 30 günün geçmiş randevuları — öğretmen, çocuk, gün-saat, durum yazıyla (Onay bekliyor / Onaylandı /
 * Reddedildi + neden / İptal edildi / Süresi doldu); öteki velinin talebinde "Talep eden". İptal düğmesi yalnızca talep eden velide açık
 * randevuda; önce onay iletişimi, işlenirken düğmeler kilitli, hata (ör. "Başlamış randevu iptal edilemez.") duyurulur ve liste yenilenir.
 * "Randevu al" talep ekranını açar; dönüşte liste yenilenir. Liste kayan öğede (T16).
 */
export function MyAppointmentsScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const { state, reload } = useMyAppointments(client);
  const [booking, setBooking] = useState(false);
  const [confirmId, setConfirmId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<{ error: boolean; text: string } | null>(null);

  if (booking) {
    return (
      <BookAppointmentScreen
        key="book"
        client={client}
        onBack={() => {
          setBooking(false);
          reload();
        }}
        onSent={(message) => {
          setBooking(false);
          setResult({ error: false, text: message });
          reload();
        }}
      />
    );
  }

  const cancel = async (id: string) => {
    setBusy(true);
    setResult(null);
    const outcome = await callApi(client.POST("/api/appointments/{id}/cancel", { params: { path: { id } }, body: { reason: null } }));
    setBusy(false);
    setConfirmId(null);
    setResult(outcome.ok ? { error: false, text: t("appointments.mine.cancelled") } : { error: true, text: failureText(outcome, "appointments.detail.error") });
    reload();
  };

  const stateText = (row: MyAppointmentRow) => t(`appointments.state.${row.state}` as TextKey);
  const row = (item: MyAppointmentRow) => (
    <View
      key={item.id}
      style={own.row}
      testID="my-appointment"
      accessible={confirmId !== item.id}
      accessibilityLabel={`${whenText(item.startsAt)}, ${item.teacherName}, ${item.studentName}, ${stateText(item)}`}
    >
      <Text style={styles.text}>{whenText(item.startsAt)}</Text>
      <Text style={styles.notice}>{fillText(t("appointments.mine.line"), { teacher: item.teacherName, student: item.studentName })}</Text>
      <Text style={styles.label}>{stateText(item)}</Text>
      {item.reason && <Text style={styles.notice}>{fillText(t("appointments.reason"), { reason: item.reason })}</Text>}
      {!item.requestedByMe && <Text style={styles.notice}>{fillText(t("appointments.mine.requestedBy"), { name: item.requestedByName })}</Text>}
      {item.canCancel && confirmId !== item.id && (
        <Pressable style={styles.secondary} accessibilityRole="button" disabled={busy} accessibilityState={{ disabled: busy }} onPress={() => setConfirmId(item.id)}>
          <Text style={styles.secondaryText}>{t("appointments.mine.cancel")}</Text>
        </Pressable>
      )}
      {item.canCancel && confirmId === item.id && (
        <View style={styles.scrollContent} accessibilityRole="alert" testID="cancel-confirm">
          <Text style={styles.text}>{t("appointments.confirm")}</Text>
          <View style={styles.actions}>
            <Pressable
              style={[styles.button, busy && styles.buttonDisabled]}
              accessibilityRole="button"
              accessibilityState={{ disabled: busy, busy }}
              disabled={busy}
              onPress={() => void cancel(item.id)}
            >
              <Text style={styles.buttonText}>{busy ? t("appointments.mine.cancelling") : t("appointments.confirmYes")}</Text>
            </Pressable>
            <Pressable style={styles.secondary} accessibilityRole="button" disabled={busy} onPress={() => setConfirmId(null)}>
              <Text style={styles.secondaryText}>{t("appointments.confirmNo")}</Text>
            </Pressable>
          </View>
        </View>
      )}
    </View>
  );

  const empty = state.kind === "ready" && state.data.upcoming.length === 0 && state.data.past.length === 0;
  return (
    <View style={[styles.card, styles.scrollCard]} testID="my-appointments">
      <Text style={styles.title} accessibilityRole="header">
        {t("appointments.mine.title")}
      </Text>
      <Pressable style={styles.button} accessibilityRole="button" disabled={busy} onPress={() => setBooking(true)}>
        <Text style={styles.buttonText}>{t("appointments.mine.book")}</Text>
      </Pressable>
      {result && (
        <Text style={result.error ? styles.error : styles.notice} accessibilityRole={result.error ? "alert" : "text"} accessibilityLiveRegion="polite">
          {result.text}
        </Text>
      )}
      {state.kind === "loading" && <Text style={styles.notice}>{t("appointments.loading")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">
            {state.message}
          </Text>
          <Pressable style={styles.button} accessibilityRole="button" onPress={reload}>
            <Text style={styles.buttonText}>{t("appointments.retry")}</Text>
          </Pressable>
        </>
      )}
      {empty && <Text style={styles.notice}>{t("appointments.mine.empty")}</Text>}
      {state.kind === "ready" && !empty && (
        <ScrollView style={styles.scrollList} contentContainerStyle={styles.scrollContent} testID="my-appointments-list">
          {state.data.upcoming.length > 0 && (
            <Text style={styles.label} accessibilityRole="header">
              {t("appointments.mine.upcoming")}
            </Text>
          )}
          {state.data.upcoming.map(row)}
          {state.data.past.length > 0 && (
            <Text style={styles.label} accessibilityRole="header">
              {t("appointments.mine.past")}
            </Text>
          )}
          {state.data.past.map(row)}
        </ScrollView>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("appointments.back")}</Text>
      </Pressable>
    </View>
  );
}
