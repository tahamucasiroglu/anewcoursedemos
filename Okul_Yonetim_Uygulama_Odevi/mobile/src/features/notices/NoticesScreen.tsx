import { useEffect, useState } from "react";
import { FlatList, Pressable, Text, View } from "react-native";
import { callApi, formatDay, markReadBody, noticeBadges, type AuthSession, type NoticeView } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; notices: NoticeView[] };

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

/**
 * Velinin bildirimleri (spec 0010 gereksinim 8, yalnızca veli): yalnızca kendi çocuklarınınkiler, en yenisi üstte, en fazla
 * 50. Okunmamışlar "Yeni" etiketiyle ve kalın yazıyla belirgin; düzeltilen yoklama "Düzeltildi" etiketiyle. Liste gelince
 * gösterilenler okundu yapılır (Hoş geldin'deki sayı sıfırlanır; arada gelen okunmamış kalır — review F-3). Durumlar: yükleniyor, boş ("Henüz bildirim yok"), dolu, hata +
 * tekrar dene. Liste kayar (FlatList), "Geri" altta sabit (0010 review F-2).
 */
export function NoticesScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/notices")).then((outcome) => {
      if (!active) {
        return;
      }

      if (outcome.ok) {
        setState({ kind: "ready", notices: outcome.data });
        const body = markReadBody(outcome.data);
        if (body && outcome.data.some((n) => !n.isRead)) {
          void callApi(client.POST("/api/notices/read", { body }));
        }
      } else {
        setState({ kind: "error", message: outcome.unreachable ? t("notices.unreachable") : outcome.message ?? t("notices.loadError") });
      }
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  return (
    <View style={[styles.card, styles.scrollCard]} testID="notices">
      <Text style={styles.title} accessibilityRole="header">
        {t("notices.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("notices.loading")}</Text>}
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
            <Text style={styles.buttonText}>{t("notices.retry")}</Text>
          </Pressable>
        </>
      )}
      {state.kind === "ready" && state.notices.length === 0 && <Text style={styles.text}>{t("notices.empty")}</Text>}
      {state.kind === "ready" && state.notices.length > 0 && (
        <FlatList
          testID="notices-list"
          style={styles.scrollList}
          contentContainerStyle={styles.scrollContent}
          data={state.notices}
          keyExtractor={(notice) => notice.id}
          renderItem={({ item: notice }) => {
            const { isNew, corrected } = noticeBadges(notice);
            const badges = [isNew ? t("notices.new") : null, corrected ? t("notices.corrected") : null].filter(Boolean).join(" · ");
            return (
              <View style={styles.secondary} testID="notice-row" accessible accessibilityLabel={[badges, formatDay(notice.day), notice.text].filter(Boolean).join(", ")}>
                <Text style={styles.muted}>{badges === "" ? formatDay(notice.day) : `${badges} · ${formatDay(notice.day)}`}</Text>
                <Text style={isNew ? styles.label : styles.text}>{notice.text}</Text>
              </View>
            );
          }}
        />
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("notices.back")}</Text>
      </Pressable>
    </View>
  );
}
