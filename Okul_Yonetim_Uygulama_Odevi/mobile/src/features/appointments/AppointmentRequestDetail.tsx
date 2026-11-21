import { useState } from "react";
import { Pressable, ScrollView, Text, TextInput, View } from "react-native";
import { callApi, fillText, validateReason, type AppointmentFieldError, type AuthSession, type ScheduleItem } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useCommunicationStyles } from "../communication/useCommunicationStyles";
import { failureText, whenText } from "./useAppointments";

interface Props {
  client: AuthSession["client"];
  item: ScheduleItem;
  onBack: () => void;
  onDone: (message: string) => void;
}

type Action = "approve" | "reject" | "cancel";

/**
 * Talep yanıtı (spec 0016 gereksinim 5–6; AC-7, AC-8, AC-13 — öğretmen): gün-saat, veli, çocuk, konu ve durum. Onay bekleyen talepte
 * "Onayla" / "Reddet" (isteğe bağlı neden, en fazla 300); onaylı randevuda "İptal et" (neden + onay iletişimi). İşlenirken düğmeler
 * kilitli; sunucu hatası (ör. "Başlamış randevu iptal edilemez.", "Bu talep zaten yanıtlandı.") duyurulur; başarıda sonuç metniyle takvime
 * döner. "Süresi doldu" talepte yalnızca bilgi.
 */
export function AppointmentRequestDetail({ client, item, onBack, onDone }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const [reason, setReason] = useState("");
  const [reasonError, setReasonError] = useState<AppointmentFieldError | undefined>(undefined);
  const [confirming, setConfirming] = useState(false);
  const [busy, setBusy] = useState<Action | null>(null);
  const [failure, setFailure] = useState<string | null>(null);
  const id = item.appointmentId ?? "";
  const canRespond = item.state === "Pending" && !item.hasStarted;
  const canCancel = item.state === "Approved" && !item.hasStarted;

  const run = async (action: Action) => {
    const error = action === "approve" ? undefined : validateReason(reason);
    setReasonError(error);
    setFailure(null);
    if (error) {
      return;
    }

    setBusy(action);
    const text = reason.trim() === "" ? null : reason.trim();
    const outcome =
      action === "cancel"
        ? await callApi(client.POST("/api/appointments/{id}/cancel", { params: { path: { id } }, body: { reason: text } }))
        : await callApi(client.POST("/api/appointments/{id}/respond", { params: { path: { id } }, body: { decision: action === "approve" ? "Approve" : "Reject", reason: text } }));
    setBusy(null);
    setConfirming(false);
    if (outcome.ok) {
      onDone(t(action === "approve" ? "appointments.detail.approved" : action === "reject" ? "appointments.detail.rejected" : "appointments.detail.cancelled"));
    } else {
      setFailure(failureText(outcome, "appointments.detail.error"));
    }
  };

  const locked = busy !== null;
  const actionButton = (action: Action, label: TextKey, primary: boolean, onPress: () => void) => (
    <Pressable
      style={[primary ? styles.button : styles.secondary, locked && styles.buttonDisabled]}
      accessibilityRole="button"
      accessibilityState={{ disabled: locked, busy: busy === action }}
      disabled={locked}
      onPress={onPress}
    >
      <Text style={primary ? styles.buttonText : styles.secondaryText}>{busy === action ? t("appointments.detail.processing") : t(label)}</Text>
    </Pressable>
  );

  return (
    <View style={[styles.card, styles.scrollCard]} testID="appointment-detail">
      <Text style={styles.title} accessibilityRole="header">
        {t("appointments.detail.title")}
      </Text>
      <ScrollView style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
        <Text style={styles.text}>{whenText(item.startsAt)}</Text>
        <Text style={styles.text}>{fillText(t("appointments.detail.state"), { state: t(`appointments.slot.${item.state}` as TextKey) })}</Text>
        {item.guardianName && <Text style={styles.text}>{fillText(t("appointments.detail.guardian"), { name: item.guardianName })}</Text>}
        {item.studentName && <Text style={styles.text}>{fillText(t("appointments.detail.student"), { name: item.studentName })}</Text>}
        {item.topic && (
          <>
            <Text style={styles.label}>{t("appointments.detail.topic")}</Text>
            <Text style={styles.text}>{item.topic}</Text>
          </>
        )}
        {(canRespond || canCancel) && (
          <>
            <Text style={styles.label}>{t("appointments.detail.reason")}</Text>
            <TextInput
              style={[styles.input, own.multiline, reasonError && styles.inputInvalid]}
              accessibilityLabel={t("appointments.detail.reason")}
              accessibilityHint={reasonError ? t(`appointments.field.${reasonError}` as TextKey) : undefined}
              value={reason}
              onChangeText={setReason}
              multiline
              editable={!locked}
            />
            {reasonError && (
              <Text style={styles.error} accessibilityRole="alert">
                {t(`appointments.field.${reasonError}` as TextKey)}
              </Text>
            )}
          </>
        )}
        {canRespond && (
          <View style={styles.actions}>
            {actionButton("approve", "appointments.detail.approve", true, () => void run("approve"))}
            {actionButton("reject", "appointments.detail.reject", false, () => void run("reject"))}
          </View>
        )}
        {canCancel && !confirming && actionButton("cancel", "appointments.detail.cancel", false, () => setConfirming(true))}
        {canCancel && confirming && (
          <View style={styles.scrollContent} accessibilityRole="alert" testID="cancel-confirm">
            <Text style={styles.text}>{t("appointments.confirm")}</Text>
            <View style={styles.actions}>
              {actionButton("cancel", "appointments.confirmYes", true, () => void run("cancel"))}
              <Pressable style={styles.secondary} accessibilityRole="button" disabled={locked} onPress={() => setConfirming(false)}>
                <Text style={styles.secondaryText}>{t("appointments.confirmNo")}</Text>
              </Pressable>
            </View>
          </View>
        )}
        {failure && (
          <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
            {failure}
          </Text>
        )}
      </ScrollView>
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button" disabled={locked}>
        <Text style={styles.secondaryText}>{t("appointments.backToSchedule")}</Text>
      </Pressable>
    </View>
  );
}
