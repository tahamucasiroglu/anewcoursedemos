import { useEffect, useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { callApi, fillText, formatDay, turkeyToday, type AnnouncementDetail, type AuthSession, type ConsentChildView } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useCommunicationStyles } from "./useCommunicationStyles";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; detail: AnnouncementDetail };
type Answer = "Approved" | "Declined";

interface Props {
  client: AuthSession["client"];
  announcementId: string;
  onBack: () => void;
}

/**
 * Duyuru ayrıntısı, veli (spec 0011 gereksinim 3, 4; AC-13): açılınca sunucu okundu yazar. Onay formunda soru, son tarih ve
 * kapsamdaki her çocuk için mevcut cevap (kim verdi) ile "Onaylıyorum" / "Onaylamıyorum"; son gün geçtiyse düğmeler kapalı
 * ve "Süre doldu". Gönderirken düğmeler kilitli; sunucu hatası (ör. "Onay süresi doldu.") duyurulur. İçerik kayar
 * (ScrollView), "Duyurulara dön" altta sabit.
 */
export function AnnouncementDetailScreen({ client, announcementId, onBack }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [sending, setSending] = useState<string | null>(null);
  const [result, setResult] = useState<{ error: boolean; text: string } | null>(null);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/announcements/{id}", { params: { path: { id: announcementId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      setState(
        outcome.ok
          ? { kind: "ready", detail: outcome.data }
          : { kind: "error", message: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("detail.loadError") },
      );
    });
    return () => {
      active = false;
    };
  }, [client, announcementId, reload]);

  const answer = async (child: ConsentChildView, value: Answer) => {
    if (state.kind !== "ready") {
      return;
    }

    setSending(child.studentId);
    setResult(null);
    const outcome = await callApi(
      client.POST("/api/announcements/{id}/consent", { params: { path: { id: announcementId } }, body: { studentId: child.studentId, answer: value } }),
    );
    setSending(null);
    if (outcome.ok) {
      const updated = outcome.data;
      setState({ kind: "ready", detail: { ...state.detail, children: state.detail.children.map((c) => (c.studentId === updated.studentId ? updated : c)) } });
      setResult({ error: false, text: t("detail.saved") });
    } else {
      setResult({ error: true, text: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("detail.saveError") });
    }
  };

  const answerText = (child: ConsentChildView) => {
    if (child.answer !== "Approved" && child.answer !== "Declined") {
      return t("detail.answer.none");
    }

    const text = t(child.answer === "Approved" ? "detail.answer.Approved" : "detail.answer.Declined");
    return child.answeredByName ? fillText(t("detail.answeredBy"), { answer: text, name: child.answeredByName }) : text;
  };

  return (
    <View style={[styles.card, styles.scrollCard]} testID="announcement-detail">
      {state.kind === "loading" && <Text style={styles.notice}>{t("detail.loading")}</Text>}
      {state.kind === "error" && (
        <>
          <Text style={styles.error} accessibilityRole="alert">{state.message}</Text>
          <Pressable
            style={styles.button}
            accessibilityRole="button"
            onPress={() => {
              setState({ kind: "loading" });
              setReload((n) => n + 1);
            }}
          >
            <Text style={styles.buttonText}>{t("communication.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && (
        <ScrollView testID="announcement-content" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          <Text style={styles.title} accessibilityRole="header">
            {state.detail.title}
          </Text>
          <Text style={styles.notice}>
            {fillText(t("detail.by"), { name: state.detail.authorName })} · {formatDay(turkeyToday(new Date(state.detail.publishedAt)))}
          </Text>
          <Text style={styles.text}>{state.detail.body}</Text>
          {state.detail.consentQuestion && (
            <>
              <Text style={styles.label}>{state.detail.consentQuestion}</Text>
              {state.detail.consentDeadline && (
                <Text style={styles.notice}>{fillText(t("detail.deadline"), { date: formatDay(state.detail.consentDeadline) })}</Text>
              )}
              {!state.detail.consentOpen && <Text style={styles.error}>{t("detail.expired")}</Text>}
              {state.detail.children.map((child) => {
                const locked = !state.detail.consentOpen || sending !== null;
                return (
                  <View key={child.studentId} style={own.row} testID="consent-child">
                    <Text style={styles.label}>{child.studentName}</Text>
                    <Text style={styles.notice}>{answerText(child)}</Text>
                    <View style={own.choices}>
                      {(["Approved", "Declined"] as const).map((value) => {
                        const chosen = child.answer === value;
                        const label = fillText(t(value === "Approved" ? "detail.approveLabel" : "detail.declineLabel"), { child: child.studentName });
                        return (
                          <Pressable
                            key={value}
                            style={[own.choice, chosen && own.chosen, locked && styles.buttonDisabled]}
                            accessibilityRole="button"
                            accessibilityLabel={label}
                            accessibilityState={{ disabled: locked, selected: chosen }}
                            disabled={locked}
                            onPress={() => void answer(child, value)}
                          >
                            <Text style={chosen ? own.chosenText : own.choiceText}>{t(value === "Approved" ? "detail.approve" : "detail.decline")}</Text>
                          </Pressable>
                        );
                      })}
                    </View>
                  </View>
                );
              })}
            </>
          )}
        </ScrollView>
      )}
      {result && (
        <Text style={result.error ? styles.error : styles.notice} accessibilityRole={result.error ? "alert" : "text"} accessibilityLiveRegion="polite">
          {result.text}
        </Text>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("detail.back")}</Text>
      </Pressable>
    </View>
  );
}
