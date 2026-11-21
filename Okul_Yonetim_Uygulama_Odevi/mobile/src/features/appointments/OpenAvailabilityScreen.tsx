import { useState } from "react";
import { Pressable, ScrollView, Text, TextInput, View } from "react-native";
import {
  APPOINTMENT_DURATIONS,
  availabilityBody,
  callApi,
  dayInputFromToday,
  fillText,
  slotCountPreview,
  turkeyToday,
  validateAvailabilityForm,
  type AppointmentFieldError,
  type AuthSession,
  type AvailabilityFormErrors,
} from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useCommunicationStyles } from "../communication/useCommunicationStyles";
import { failureText } from "./useAppointments";

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
  onOpened: (count: number) => void;
}

/**
 * "Müsaitlik aç" (spec 0016 gereksinim 1; AC-1, AC-2, AC-13 — öğretmen): gün ("GG.AA.YYYY" + "Bugün"/"Yarın"), başlangıç ve bitiş
 * ("SS:DD", Türkiye saati), süre için üç düğme (15/20/30). Alan hatası varsa istek gitmez; gönderilirken düğme kilitli; sunucu hatası
 * (ör. "Bu saatlerde zaten müsaitliğiniz var.") duyurulur; başarıda açılan dilim sayısıyla takvime döner. Saat seçici paketi yok (plan
 * Risks 7). Kayan içerik, "Geri" altta sabit.
 */
export function OpenAvailabilityScreen({ client, onBack, onOpened }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const today = turkeyToday();
  const [day, setDay] = useState(dayInputFromToday(today, 1));
  const [start, setStart] = useState("14:00");
  const [end, setEnd] = useState("16:00");
  const [duration, setDuration] = useState<number | null>(20);
  const [errors, setErrors] = useState<AvailabilityFormErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [failure, setFailure] = useState<string | null>(null);

  const submit = async () => {
    const values = { day, start, end, durationMinutes: duration };
    const found = validateAvailabilityForm(values, today);
    setErrors(found);
    setFailure(null);
    if (Object.keys(found).length > 0) {
      return;
    }

    setSubmitting(true);
    const outcome = await callApi(client.POST("/api/appointments/availability", { body: availabilityBody(values) }));
    setSubmitting(false);
    if (outcome.ok) {
      onOpened(Number(outcome.data.slotCount));
    } else {
      setFailure(failureText(outcome, "appointments.open.error"));
    }
  };

  const fieldText = (error: AppointmentFieldError) => t(`appointments.field.${error}` as TextKey);
  const field = (label: string, value: string, onChange: (v: string) => void, error: AppointmentFieldError | undefined, maxLength: number) => (
    <>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        style={[styles.input, error && styles.inputInvalid]}
        accessibilityLabel={label}
        accessibilityHint={error ? fieldText(error) : undefined}
        value={value}
        onChangeText={onChange}
        keyboardType="numbers-and-punctuation"
        maxLength={maxLength}
        editable={!submitting}
      />
      {error && (
        <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
          {fieldText(error)}
        </Text>
      )}
    </>
  );

  const preview = slotCountPreview(start, end, duration);
  return (
    <View style={[styles.card, styles.scrollCard]} testID="open-availability">
      <Text style={styles.title} accessibilityRole="header">
        {t("appointments.open.title")}
      </Text>
      <ScrollView style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
        {field(t("appointments.open.day"), day, setDay, errors.day, 10)}
        <View style={own.choices}>
          <Pressable style={own.choice} accessibilityRole="button" disabled={submitting} onPress={() => setDay(dayInputFromToday(today, 0))}>
            <Text style={own.choiceText}>{t("appointments.open.today")}</Text>
          </Pressable>
          <Pressable style={own.choice} accessibilityRole="button" disabled={submitting} onPress={() => setDay(dayInputFromToday(today, 1))}>
            <Text style={own.choiceText}>{t("appointments.open.tomorrow")}</Text>
          </Pressable>
        </View>
        {field(t("appointments.open.start"), start, setStart, errors.start, 5)}
        {field(t("appointments.open.end"), end, setEnd, errors.end, 5)}
        <Text style={styles.label}>{t("appointments.open.duration")}</Text>
        <View style={own.choices}>
          {APPOINTMENT_DURATIONS.map((minutes) => {
            const chosen = duration === minutes;
            return (
              <Pressable
                key={minutes}
                style={[own.choice, chosen && own.chosen]}
                accessibilityRole="button"
                accessibilityLabel={fillText(t("appointments.open.minutesLabel"), { n: minutes })}
                accessibilityState={{ selected: chosen, disabled: submitting }}
                disabled={submitting}
                onPress={() => setDuration(minutes)}
              >
                <Text style={chosen ? own.chosenText : own.choiceText}>{fillText(t("appointments.open.minutes"), { n: minutes })}</Text>
              </Pressable>
            );
          })}
        </View>
        {errors.durationMinutes && (
          <Text style={styles.error} accessibilityRole="alert">
            {fieldText(errors.durationMinutes)}
          </Text>
        )}
        {preview > 0 && <Text style={styles.notice}>{fillText(t("appointments.open.preview"), { n: preview })}</Text>}
        <Pressable
          style={[styles.button, submitting && styles.buttonDisabled]}
          accessibilityRole="button"
          accessibilityState={{ disabled: submitting, busy: submitting }}
          disabled={submitting}
          onPress={() => void submit()}
        >
          <Text style={styles.buttonText}>{submitting ? t("appointments.open.submitting") : t("appointments.open.submit")}</Text>
        </Pressable>
        {failure && (
          <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
            {failure}
          </Text>
        )}
      </ScrollView>
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button" disabled={submitting}>
        <Text style={styles.secondaryText}>{t("appointments.back")}</Text>
      </Pressable>
    </View>
  );
}
