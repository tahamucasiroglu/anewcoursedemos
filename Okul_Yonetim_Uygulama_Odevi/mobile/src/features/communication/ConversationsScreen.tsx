import { useEffect, useState } from "react";
import { FlatList, Pressable, RefreshControl, Text, View } from "react-native";
import { callApi, fillText, type AuthSession, type ConversationRow } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { ConversationScreen } from "./ConversationScreen";
import { NewConversationScreen } from "./NewConversationScreen";
import { useCommunicationStyles } from "./useCommunicationStyles";

type State = { kind: "loading" } | { kind: "error"; message: string } | { kind: "ready"; rows: ConversationRow[] };
type Mode = { kind: "list" } | { kind: "new" } | { kind: "open"; id: string };

interface Props {
  client: AuthSession["client"];
  onBack: () => void;
}

/**
 * Mesajlar (spec 0011 gereksinim 5; AC-13 — veli ve öğretmen): konuşmalar son mesaja göre, karşı taraf, öğrenci ve okunmamış
 * sayısıyla ("3 okunmamış mesaj"); dokununca konuşma açılır, "Yeni mesaj" ile kişi seçilir. Açılınca, konuşmadan dönünce ve
 * aşağı çekince yenilenir. Durumlar: yükleniyor, boş ("Henüz mesaj yok"), dolu, hata + tekrar dene. Liste kayar, "Yeni mesaj"
 * ve "Geri" altta sabit (conventions T16).
 */
export function ConversationsScreen({ client, onBack }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [refreshing, setRefreshing] = useState(false);
  const [view, setView] = useState<Mode>({ kind: "list" });

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/conversations")).then((outcome) => {
      if (!active) {
        return;
      }

      setRefreshing(false);
      setState(
        outcome.ok
          ? { kind: "ready", rows: outcome.data }
          : { kind: "error", message: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("messages.loadError") },
      );
    });
    return () => {
      active = false;
    };
  }, [client, reload]);

  const backToList = () => {
    setView({ kind: "list" });
    setReload((n) => n + 1);
  };

  if (view.kind === "open") {
    return <ConversationScreen key={view.id} client={client} conversationId={view.id} onBack={backToList} />;
  }

  if (view.kind === "new") {
    return <NewConversationScreen client={client} onStarted={(row) => setView({ kind: "open", id: row.id })} onBack={backToList} />;
  }

  return (
    <View style={[styles.card, styles.scrollCard]} testID="conversations">
      <Text style={styles.title} accessibilityRole="header">
        {t("messages.title")}
      </Text>
      {state.kind === "loading" && <Text style={styles.notice}>{t("messages.loading")}</Text>}
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
      {state.kind === "ready" && state.rows.length === 0 && <Text style={styles.text}>{t("messages.empty")}</Text>}
      {state.kind === "ready" && state.rows.length > 0 && (
        <FlatList
          testID="conversations-list"
          style={styles.scrollList}
          contentContainerStyle={styles.scrollContent}
          data={state.rows}
          keyExtractor={(row) => row.id}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={() => {
                setRefreshing(true);
                setReload((n) => n + 1);
              }}
            />
          }
          renderItem={({ item: row }) => {
            const unread = Number(row.unreadCount);
            const unreadText = fillText(t("messages.unread"), { n: unread });
            return (
              <Pressable
                style={own.row}
                testID="conversation-row"
                accessibilityRole="button"
                accessibilityLabel={[row.counterpartName, row.studentName, unread > 0 ? unreadText : null].filter(Boolean).join(", ")}
                onPress={() => setView({ kind: "open", id: row.id })}
              >
                <Text style={unread > 0 ? styles.label : styles.text}>{row.counterpartName}</Text>
                <Text style={styles.notice}>{row.studentName}</Text>
                {unread > 0 && <Text style={own.badge} accessibilityLabel={unreadText}>{String(unread)}</Text>}
              </Pressable>
            );
          }}
        />
      )}
      <View style={styles.actions}>
        <Pressable style={styles.button} onPress={() => setView({ kind: "new" })} accessibilityRole="button">
          <Text style={styles.buttonText}>{t("messages.new")}</Text>
        </Pressable>
      </View>
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("communication.welcomeBack")}</Text>
      </Pressable>
    </View>
  );
}
