import { useEffect } from "react";
import { Pressable, Text, View } from "react-native";
import type { AuthSession, VoiceMessageDraft } from "@okul/api-client";
import { t } from "../../i18n/t";
import { useAuthStyles } from "../auth/useAuthStyles";
import { useVoiceNote } from "./useVoiceNote";

interface Props {
  client: AuthSession["client"];
  conversationId: string;
  onDraft: (draft: VoiceMessageDraft) => void;
  /** Kayıt ya da dönüştürme sürerken true — konuşma ekranı "Gönder"i kilitler. */
  onBusyChange: (busy: boolean) => void;
}

/**
 * Öğretmenin "Sesli not" düğmesi (spec 0012 AC-12): kayıt yokken düğme; kaydederken geçen saniye ve "Durdur" (60 sn'de
 * kendiliğinden durur); dönüştürülürken "Dönüştürülüyor…"; hata (kullanılamıyor, anlaşılamadı, 60 sn, mikrofon izni yok)
 * düğmenin altında. Sayaç ve hata ekran okuyucuya duyurulur; düğmelerde erişilebilirlik etiketi var.
 */
export function VoiceNoteButton({ client, conversationId, onDraft, onBusyChange }: Props) {
  const styles = useAuthStyles();
  const { state, start, stop } = useVoiceNote(client, conversationId, onDraft);
  const busy = state.kind === "recording" || state.kind === "converting";

  useEffect(() => {
    onBusyChange(busy);
  }, [busy, onBusyChange]);

  // Taslak gelince düğme kaldırılır; kilit açık kalsın.
  useEffect(() => () => onBusyChange(false), [onBusyChange]);

  if (state.kind === "recording") {
    return (
      <View testID="voice-recording">
        <Text style={styles.notice} accessibilityLiveRegion="polite" accessibilityRole="text">
          {t("voice.recording").replace("{n}", String(state.seconds))}
        </Text>
        <Pressable style={styles.button} accessibilityRole="button" accessibilityLabel={t("voice.stop")} onPress={() => void stop()}>
          <Text style={styles.buttonText}>{t("voice.stop")}</Text>
        </Pressable>
      </View>
    );
  }

  if (state.kind === "converting") {
    return (
      <Text style={styles.notice} testID="voice-converting" accessibilityLiveRegion="polite" accessibilityState={{ busy: true }}>
        {t("voice.converting")}
      </Text>
    );
  }

  return (
    <View>
      <Pressable
        style={styles.secondary}
        accessibilityRole="button"
        accessibilityLabel={t("voice.record")}
        accessibilityHint={t("voice.recordHint")}
        onPress={() => void start()}
      >
        <Text style={styles.secondaryText}>{t("voice.record")}</Text>
      </Pressable>
      {state.kind === "error" && (
        <Text style={styles.error} accessibilityRole="alert" accessibilityLiveRegion="assertive">
          {state.message}
        </Text>
      )}
    </View>
  );
}
