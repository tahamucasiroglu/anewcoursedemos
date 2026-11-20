import { useEffect, useState } from "react";
import { FlatList, Pressable, Text, TextInput, View } from "react-native";
import { callApi, validateMessage, type AuthSession, type CommunicationFieldError, type ConversationRow, type MessageView } from "@okul/api-client";
import { t, type TextKey } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useCommunicationStyles } from "./useCommunicationStyles";

type State =
  | { kind: "loading" }
  | { kind: "error"; message: string }
  | { kind: "ready"; conversation: ConversationRow; canSend: boolean; messages: MessageView[]; hasMore: boolean };

interface Props {
  client: AuthSession["client"];
  conversationId: string;
  onBack: () => void;
}

/**
 * Tek konuşma (spec 0011 gereksinim 5, 6; AC-13): son 50 mesaj eskiden yeniye, "Daha eski" ile önceki 50 (açılınca okundu
 * olur). Yazma: boş ya da 2000'i aşan mesaj alan hatası verir, istek gitmez; gönderirken düğme kilitli; sunucu hatası
 * duyurulur; öğretmen mesai dışındaysa "mesajınız iletildi" notu. İlişki bittiyse alan kapalı ve "Bu konuşmaya artık mesaj
 * gönderilemez.". Mesajlar kayar (FlatList), yazma alanı ve "Geri" altta sabit.
 */
export function ConversationScreen({ client, conversationId, onBack }: Props) {
  const styles = useAuthStyles();
  const own = useCommunicationStyles();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reload, setReload] = useState(0);
  const [text, setText] = useState("");
  const [fieldError, setFieldError] = useState<CommunicationFieldError | undefined>();
  const [sending, setSending] = useState(false);
  const [loadingOlder, setLoadingOlder] = useState(false);
  const [note, setNote] = useState<{ error: boolean; text: string } | null>(null);

  useEffect(() => {
    let active = true;
    callApi(client.GET("/api/conversations/{id}/messages", { params: { path: { id: conversationId } } })).then((outcome) => {
      if (!active) {
        return;
      }

      setState(
        outcome.ok
          ? { kind: "ready", conversation: outcome.data.conversation, canSend: outcome.data.canSend, messages: outcome.data.messages, hasMore: outcome.data.hasMore }
          : { kind: "error", message: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("messages.loadError") },
      );
    });
    return () => {
      active = false;
    };
  }, [client, conversationId, reload]);

  const loadOlder = async () => {
    if (state.kind !== "ready" || state.messages.length === 0) {
      return;
    }

    setLoadingOlder(true);
    const outcome = await callApi(
      client.GET("/api/conversations/{id}/messages", { params: { path: { id: conversationId }, query: { before: state.messages[0].id } } }),
    );
    setLoadingOlder(false);
    if (outcome.ok) {
      setState({ ...state, messages: [...outcome.data.messages, ...state.messages], hasMore: outcome.data.hasMore });
    }
  };

  const send = async () => {
    if (state.kind !== "ready") {
      return;
    }

    const error = validateMessage(text);
    setFieldError(error);
    setNote(null);
    if (error) {
      return;
    }

    setSending(true);
    const outcome = await callApi(client.POST("/api/conversations/{id}/messages", { params: { path: { id: conversationId } }, body: { body: text.trim() } }));
    setSending(false);
    if (outcome.ok) {
      setState({ ...state, messages: [...state.messages, outcome.data.message] });
      setText("");
      if (outcome.data.outsideWorkingHours) {
        setNote({ error: false, text: t("messages.outsideHours") });
      }
    } else {
      setNote({ error: true, text: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("messages.sendError") });
    }
  };

  return (
    <View style={[styles.card, styles.scrollCard]} testID="conversation">
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
      {state.kind === "ready" && (
        <>
          <Text style={styles.title} accessibilityRole="header">
            {state.conversation.counterpartName}
          </Text>
          <Text style={styles.notice}>{state.conversation.studentName}</Text>
          <FlatList
            testID="messages-list"
            style={styles.scrollList}
            contentContainerStyle={styles.scrollContent}
            data={state.messages}
            keyExtractor={(message) => message.id}
            ListHeaderComponent={
              state.hasMore ? (
                <Pressable style={styles.secondary} accessibilityRole="button" disabled={loadingOlder} accessibilityState={{ disabled: loadingOlder, busy: loadingOlder }} onPress={() => void loadOlder()}>
                  <Text style={styles.secondaryText}>{t("messages.older")}</Text>
                </Pressable>
              ) : undefined
            }
            renderItem={({ item: message }) => (
              <View style={message.mine ? own.mine : own.theirs} testID="message" accessible accessibilityLabel={`${message.mine ? t("messages.mine") : state.conversation.counterpartName}: ${message.body}`}>
                <Text style={message.mine ? own.mineText : own.theirsText}>{message.body}</Text>
              </View>
            )}
          />
          {!state.canSend && <Text style={styles.error} accessibilityRole="alert">{t("messages.cannotSend")}</Text>}
          <TextInput
            style={[styles.input, own.multiline, fieldError && styles.inputInvalid]}
            accessibilityLabel={t("messages.input")}
            accessibilityHint={fieldError ? t(`messages.field.${fieldError}` as TextKey) : undefined}
            value={text}
            onChangeText={setText}
            editable={state.canSend && !sending}
            multiline
          />
          {fieldError && (
            <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
              {t(`messages.field.${fieldError}` as TextKey)}
            </Text>
          )}
          {note && (
            <Text style={note.error ? styles.error : styles.notice} accessibilityRole={note.error ? "alert" : "text"} accessibilityLiveRegion="polite">
              {note.text}
            </Text>
          )}
          <Pressable
            style={[styles.button, (sending || !state.canSend) && styles.buttonDisabled]}
            accessibilityRole="button"
            accessibilityState={{ disabled: sending || !state.canSend, busy: sending }}
            disabled={sending || !state.canSend}
            onPress={() => void send()}
          >
            <Text style={styles.buttonText}>{sending ? t("messages.sending") : t("messages.send")}</Text>
          </Pressable>
        </>
      )}
      <Pressable style={styles.secondary} onPress={onBack} accessibilityRole="button">
        <Text style={styles.secondaryText}>{t("messages.back")}</Text>
      </Pressable>
    </View>
  );
}
