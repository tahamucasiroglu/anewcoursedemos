import { useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { callApi, fillText, groupByTurkeyDay, turkeyTimeOf, type AuthSession, type ScheduleItem } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useCommunicationStyles } from "../communication/useCommunicationStyles";
import { AppointmentRequestDetail } from "./AppointmentRequestDetail";
import { OpenAvailabilityScreen } from "./OpenAvailabilityScreen";
import { dayHeaderText, failureText, useSchedule, whenText } from "./useAppointments";

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

type Mode = { kind: "list" } | { kind: "open" } | { kind: "detail"; item: ScheduleItem };

/**
 * "Takvimim" (spec 0016 gereksinim 2; AC-3, AC-13 — öğretmen): yükleniyor / hata + tekrar dene / boş ("Henüz müsaitlik açmadınız") /
 * dolu. Üstte onay bekleyen talepler (dokununca talep yanıtı), altında gün gün dilimler; dilim düğmesinin etiketi gün, saat ve durumu
 * içerir ("Perşembe 26 Kasım 14:20, boş" — durum yalnızca renkle değil yazıyla). Boş ve başlamamış dilimde "Kaldır" (işlenirken düğmeler
 * kilitli; hata ya da sonuç duyurulur), onaylı dilimde talep ekranından "İptal et". "Müsaitlik aç" formu açar; dönüşte liste yenilenir
 * (eski ekran kalmaz). Liste kayan öğede (T16); başlık ve "Geri" sabit.
 */
export function TeacherScheduleScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const { state, reload } = useSchedule(client);
  const [view, setView] = useState<Mode>({ kind: "list" });
  const [busy, setBusy] = useState<string | null>(null);
  const [result, setResult] = useState<{ error: boolean; text: string } | null>(null);

  const backToList = (message?: string) => {
    setView({ kind: "list" });
    setResult(message ? { error: false, text: message } : null);
    reload();
  };

  if (view.kind === "open") {
    return <OpenAvailabilityScreen key="open" client={client} onBack={() => backToList()} onOpened={(count) => backToList(fillText(t("appointments.schedule.opened"), { n: count }))} />;
  }

  if (view.kind === "detail") {
    return <AppointmentRequestDetail key={view.item.appointmentId ?? view.item.slotId} client={client} item={view.item} onBack={() => backToList()} onDone={(message) => backToList(message)} />;
  }

  const remove = async (slotId: string) => {
    setBusy(slotId);
    setResult(null);
    const outcome = await callApi(client.DELETE("/api/appointments/slots/{id}", { params: { path: { id: slotId } } }));
    setBusy(null);
    if (outcome.ok) {
      backToList(t("appointments.schedule.removed"));
    } else {
      setResult({ error: true, text: failureText(outcome, "appointments.schedule.removeError") });
      reload();
    }
  };

  const slotLabel = (item: ScheduleItem) => `${whenText(item.startsAt)}, ${t(`appointments.slot.${item.state}` as TextKey)}`;

  return (
    <View style={[styles.card, styles.scrollCard]} testID="teacher-schedule">
      <Text style={styles.title} accessibilityRole="header">
        {t("appointments.schedule.title")}
      </Text>
      <Pressable style={styles.button} accessibilityRole="button" disabled={busy !== null} onPress={() => setView({ kind: "open" })}>
        <Text style={styles.buttonText}>{t("appointments.schedule.open")}</Text>
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
      {state.kind === "ready" && state.data.slots.length === 0 && <Text style={styles.notice}>{t("appointments.schedule.empty")}</Text>}
      {state.kind === "ready" && state.data.slots.length > 0 && (
        <ScrollView style={styles.scrollList} contentContainerStyle={styles.scrollContent} testID="schedule-list">
          {state.data.pending.length > 0 && (
            <>
              <Text style={styles.label} accessibilityRole="header">
                {t("appointments.schedule.pending")}
              </Text>
              {state.data.pending.map((item) => (
                <Pressable
                  key={`p-${item.slotId}`}
                  style={own.row}
                  accessibilityRole="button"
                  accessibilityLabel={`${slotLabel(item)}, ${item.guardianName ?? ""}, ${item.studentName ?? ""}`}
                  onPress={() => setView({ kind: "detail", item })}
                  testID="pending-request"
                >
                  <Text style={styles.text}>{whenText(item.startsAt)}</Text>
                  <Text style={styles.notice}>{fillText(t("appointments.requestLine"), { guardian: item.guardianName ?? "", student: item.studentName ?? "" })}</Text>
                  <Text style={styles.notice} numberOfLines={2}>
                    {item.topic}
                  </Text>
                </Pressable>
              ))}
            </>
          )}
          {groupByTurkeyDay(state.data.slots, (s) => s.startsAt).map((group) => (
            <View key={group.day} style={styles.scrollContent}>
              <Text style={styles.label} accessibilityRole="header">
                {dayHeaderText(group.day)}
              </Text>
              {group.items.map((item) => {
                const removable = item.state === "Free" && !item.hasStarted;
                const opensDetail = item.state === "Pending" || item.state === "Approved" || item.state === "Expired";
                return (
                  <View key={item.slotId} style={own.row} testID="schedule-slot">
                    <Pressable
                      accessibilityRole="button"
                      accessibilityLabel={slotLabel(item)}
                      disabled={!opensDetail || busy !== null}
                      onPress={() => setView({ kind: "detail", item })}
                    >
                      <Text style={styles.text}>
                        {turkeyTimeOf(item.startsAt)} · {t(`appointments.slot.${item.state}` as TextKey)}
                      </Text>
                      {item.studentName && <Text style={styles.notice}>{fillText(t("appointments.requestLine"), { guardian: item.guardianName ?? "", student: item.studentName })}</Text>}
                    </Pressable>
                    {removable && (
                      <Pressable
                        style={[own.choice, busy !== null && styles.buttonDisabled]}
                        accessibilityRole="button"
                        accessibilityLabel={`${t("appointments.schedule.remove")}: ${slotLabel(item)}`}
                        accessibilityState={{ disabled: busy !== null, busy: busy === item.slotId }}
                        disabled={busy !== null}
                        onPress={() => void remove(item.slotId)}
                      >
                        <Text style={own.choiceText}>{busy === item.slotId ? t("appointments.schedule.removing") : t("appointments.schedule.remove")}</Text>
                      </Pressable>
                    )}
                  </View>
                );
              })}
            </View>
          ))}
        </ScrollView>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("appointments.back")}</Text>
      </Pressable>
    </View>
  );
}
