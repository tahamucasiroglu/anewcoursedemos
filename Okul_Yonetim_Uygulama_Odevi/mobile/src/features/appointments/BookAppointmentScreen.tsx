import { useState } from "react";
import { Pressable, ScrollView, Text, TextInput, View } from "react-native";
import { callApi, groupByTurkeyDay, turkeyTimeOf, validateTopic, type AppointmentFieldError, type AuthSession, type OpenSlot } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useChildren } from "../children/useChildren";
import { useCommunicationStyles } from "../communication/useCommunicationStyles";
import { dayHeaderText, failureText, useBookableTeachers, useOpenSlots, whenText } from "./useAppointments";

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
  onSent: (message: string) => void;
}

/**
 * "Randevu al" (spec 0016 gereksinim 3–4; AC-4, AC-5, AC-13 — veli): [birden çok çocuk] çocuk seç → öğretmen seç → boş saatler (gün gün;
 * düğme etiketi gün, saat ve "boş") → konu → "Talep gönder". Boş durumlar: "Kayıtlı çocuğunuz yok", "Bu sınıfa atanmış öğretmen yok",
 * "Bu öğretmenin boş görüşme saati yok". Konu hatasında istek gitmez; gönderilirken düğme kilitli; sunucu hatasında (ör. "Bu saat artık
 * dolu.", açık randevu var) mesaj duyurulur ve boş saatler yenilenir (eski liste kalmaz); başarıda "Talebiniz öğretmene iletildi" ile
 * Randevularım'a döner. Listeler kayan öğede (T16).
 */
export function BookAppointmentScreen({ client, onBack, onSent }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const children = useChildren(client);
  const [childId, setChildId] = useState<string | null>(null);
  const [teacherId, setTeacherId] = useState<string | null>(null);
  const [slot, setSlot] = useState<OpenSlot | null>(null);
  const [topic, setTopic] = useState("");
  const [topicError, setTopicError] = useState<AppointmentFieldError | undefined>(undefined);
  const [sending, setSending] = useState(false);
  const [failure, setFailure] = useState<string | null>(null);
  const kids = children.state.kind === "ready" ? children.state.children : [];
  const studentId = childId ?? (kids.length === 1 ? kids[0].id : null);
  const teachers = useBookableTeachers(client, studentId);
  const slots = useOpenSlots(client, teacherId, studentId);

  const chooseChild = (id: string) => {
    setChildId(id);
    setTeacherId(null);
    setSlot(null);
    setFailure(null);
    teachers.reload();
  };

  const chooseTeacher = (id: string) => {
    setTeacherId(id);
    setSlot(null);
    setFailure(null);
    slots.reload();
  };

  const send = async () => {
    const error = validateTopic(topic);
    setTopicError(error);
    setFailure(null);
    if (error || !slot || !studentId) {
      return;
    }

    setSending(true);
    const outcome = await callApi(client.POST("/api/appointments", { body: { slotId: slot.slotId, studentId, topic: topic.trim() } }));
    setSending(false);
    if (outcome.ok) {
      onSent(t("appointments.book.sent"));
    } else {
      setFailure(failureText(outcome, "appointments.book.error"));
      setSlot(null);
      slots.reload();
    }
  };

  const loadError = (message: string, retry: () => void) => (
    <>
      <Text style={styles.error} accessibilityRole="alert">
        {message}
      </Text>
      <Pressable style={styles.button} accessibilityRole="button" onPress={retry}>
        <Text style={styles.buttonText}>{t("appointments.retry")}</Text>
      </Pressable>
    </>
  );

  return (
    <View style={[styles.card, styles.scrollCard]} testID="book-appointment">
      <Text style={styles.title} accessibilityRole="header">
        {t("appointments.book.title")}
      </Text>
      <ScrollView style={styles.scrollList} contentContainerStyle={styles.scrollContent} testID="book-list">
        {children.state.kind === "loading" && <Text style={styles.notice}>{t("appointments.loading")}</Text>}
        {children.state.kind === "error" && loadError(children.state.message, children.retry)}
        {children.state.kind === "ready" && kids.length === 0 && <Text style={styles.notice}>{t("appointments.book.noChildren")}</Text>}
        {kids.length > 1 && (
          <>
            <Text style={styles.label}>{t("appointments.book.child")}</Text>
            <View style={own.choices}>
              {kids.map((kid) => {
                const chosen = studentId === kid.id;
                return (
                  <Pressable key={kid.id} style={[own.choice, chosen && own.chosen]} accessibilityRole="button" accessibilityState={{ selected: chosen }} disabled={sending} onPress={() => chooseChild(kid.id)}>
                    <Text style={chosen ? own.chosenText : own.choiceText}>{kid.fullName}</Text>
                  </Pressable>
                );
              })}
            </View>
          </>
        )}
        {studentId !== null && (
          <>
            <Text style={styles.label}>{t("appointments.book.teacher")}</Text>
            {teachers.state.kind === "loading" && <Text style={styles.notice}>{t("appointments.loading")}</Text>}
            {teachers.state.kind === "error" && loadError(teachers.state.message, teachers.reload)}
            {teachers.state.kind === "ready" && teachers.state.data.length === 0 && <Text style={styles.notice}>{t("appointments.book.noTeachers")}</Text>}
            {teachers.state.kind === "ready" && (
              <View style={own.choices}>
                {teachers.state.data.map((teacher) => {
                  const chosen = teacherId === teacher.userId;
                  return (
                    <Pressable key={teacher.userId} style={[own.choice, chosen && own.chosen]} accessibilityRole="button" accessibilityState={{ selected: chosen }} disabled={sending} onPress={() => chooseTeacher(teacher.userId)}>
                      <Text style={chosen ? own.chosenText : own.choiceText}>{teacher.fullName}</Text>
                    </Pressable>
                  );
                })}
              </View>
            )}
          </>
        )}
        {failure && (
          <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
            {failure}
          </Text>
        )}
        {teacherId !== null && slot === null && (
          <>
            <Text style={styles.label}>{t("appointments.book.slots")}</Text>
            {slots.state.kind === "loading" && <Text style={styles.notice}>{t("appointments.loading")}</Text>}
            {slots.state.kind === "error" && loadError(slots.state.message, slots.reload)}
            {slots.state.kind === "ready" && slots.state.data.length === 0 && <Text style={styles.notice}>{t("appointments.book.noSlots")}</Text>}
            {slots.state.kind === "ready" &&
              groupByTurkeyDay(slots.state.data, (s) => s.startsAt).map((group) => (
                <View key={group.day} style={styles.scrollContent}>
                  <Text style={styles.label} accessibilityRole="header">
                    {dayHeaderText(group.day)}
                  </Text>
                  <View style={own.choices}>
                    {group.items.map((open) => (
                      <Pressable
                        key={open.slotId}
                        style={own.choice}
                        accessibilityRole="button"
                        accessibilityLabel={`${whenText(open.startsAt)}, ${t("appointments.slot.Free")}`}
                        onPress={() => {
                          setSlot(open);
                          setFailure(null);
                        }}
                        testID="open-slot"
                      >
                        <Text style={own.choiceText}>{turkeyTimeOf(open.startsAt)}</Text>
                      </Pressable>
                    ))}
                  </View>
                </View>
              ))}
          </>
        )}
        {slot !== null && (
          <>
            <Text style={styles.text}>{`${t("appointments.book.chosen")} ${whenText(slot.startsAt)}`}</Text>
            <Pressable style={styles.secondary} accessibilityRole="button" disabled={sending} onPress={() => setSlot(null)}>
              <Text style={styles.secondaryText}>{t("appointments.book.otherSlot")}</Text>
            </Pressable>
            <Text style={styles.label}>{t("appointments.book.topic")}</Text>
            <TextInput
              style={[styles.input, own.multiline, topicError && styles.inputInvalid]}
              accessibilityLabel={t("appointments.book.topic")}
              accessibilityHint={topicError ? t(`appointments.field.${topicError}` as TextKey) : undefined}
              value={topic}
              onChangeText={setTopic}
              multiline
              editable={!sending}
            />
            {topicError && (
              <Text style={styles.error} accessibilityRole="alert">
                {t(`appointments.field.${topicError}` as TextKey)}
              </Text>
            )}
            <Pressable
              style={[styles.button, sending && styles.buttonDisabled]}
              accessibilityRole="button"
              accessibilityState={{ disabled: sending, busy: sending }}
              disabled={sending}
              onPress={() => void send()}
            >
              <Text style={styles.buttonText}>{sending ? t("appointments.book.submitting") : t("appointments.book.submit")}</Text>
            </Pressable>
          </>
        )}
      </ScrollView>
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button" disabled={sending}>
        <Text style={styles.secondaryText}>{t("appointments.back")}</Text>
      </Pressable>
    </View>
  );
}
