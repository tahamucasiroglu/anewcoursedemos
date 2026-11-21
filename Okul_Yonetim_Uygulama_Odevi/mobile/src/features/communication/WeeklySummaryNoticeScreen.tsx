import { useEffect, useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { callApi, formatDay, turkeyToday, type AuthSession, type NoticeView } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; notice: NoticeView };

interface Props {
  client: AuthSession["client"];
  noticeId: string;
  onBack: () => void;
}

/**
 * Haftalık gelişim özeti ayrıntısı, veli (spec 0014 gereksinim 7, AC-10): başlık ("Haftalık gelişim özeti — 16–22 Kasım"), çocuğun
 * adı ve gönderim günü, öğretmenin onayladığı özet ile altındaki "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı." etiketi ve
 * dayanak satırı (sunucunun hazır metni, satır sonları korunur). Açılınca sunucu okundu yazar; yalnızca kendi çocuğunun özeti açılır
 * (başkasınınki sunucuda 404 — hata + tekrar dene). İçerik kayar (ScrollView, conventions T16), "Duyurulara dön" altta sabit.
 */
export function WeeklySummaryNoticeScreen({ client, noticeId, onBack }: Props) {
  const styles = useAuthStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/notices/{id}", { params: { path: { id: noticeId } } })).then((outcome) => {
      if (active) {
        setState(outcome.ok ? { kind: "ready", notice: outcome.data } : { kind: "error", message: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("weekly.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, noticeId, reload]);

  return (
    <View style={[styles.card, styles.scrollCard]} testID="weekly-summary-detail">
      {state.kind === "loading" && <Text style={styles.notice}>{t("weekly.loading")}</Text>}
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
        <ScrollView testID="weekly-summary-content" style={styles.scrollList} contentContainerStyle={styles.scrollContent}>
          <Text style={styles.title} accessibilityRole="header">
            {state.notice.title ?? t("weekly.title")}
          </Text>
          <Text style={styles.notice}>
            {state.notice.studentName} · {formatDay(turkeyToday(new Date(state.notice.at)))}
          </Text>
          <Text style={styles.text} testID="weekly-summary-text">{state.notice.text}</Text>
        </ScrollView>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("detail.back")}</Text>
      </Pressable>
    </View>
  );
}
