import { useCallback, useEffect, useRef, useState } from "react";
import { RecordingPresets, requestRecordingPermissionsAsync, setAudioModeAsync, useAudioRecorder } from "expo-audio";
import { uploadVoiceNote, VOICE_NOTE_MAX_SECONDS, type AuthSession, type VoiceMessageDraft } from "@okul/api-client";
import { t } from "../../i18n/t";

export type VoiceNoteState =
  | { kind: "idle" }
  | { kind: "recording"; seconds: number }
  | { kind: "converting" }
  | { kind: "error"; message: string };

/**
 * Sesli not kaydı ve dönüştürme (spec 0012 C-3, AC-12): mikrofon izni istenir (yoksa "Mikrofon izni verilmedi."), kayıt
 * başlar ve geçen saniye sayılır; "Durdur"da ya da 60. saniyede kendiliğinden durur, ses sunucuya yüklenir ("Dönüştürülüyor…")
 * ve gelen taslak <c>onDraft</c>'a verilir. Sunucunun Türkçe hatası (kullanılamıyor, anlaşılamadı, 60 sn) olduğu gibi
 * gösterilir. Ses dosyası yalnızca cihazın kayıt önbelleğindedir; uygulama başka yere yazmaz.
 */
export function useVoiceNote(client: AuthSession["client"], conversationId: string, onDraft: (draft: VoiceMessageDraft) => void) {
  const recorder = useAudioRecorder(RecordingPresets.HIGH_QUALITY);
  const [state, setState] = useState<VoiceNoteState>({ kind: "idle" });
  const timer = useRef<ReturnType<typeof setInterval> | null>(null);
  const stopping = useRef(false);

  const clearTimer = () => {
    if (timer.current !== null) {
      clearInterval(timer.current);
      timer.current = null;
    }
  };

  useEffect(() => clearTimer, []);

  const stop = useCallback(async () => {
    if (stopping.current) {
      return;
    }

    stopping.current = true;
    clearTimer();
    setState({ kind: "converting" });
    try {
      await recorder.stop();
    } catch {
      // Kayıt zaten durmuşsa yine de elde olanla devam edilir.
    }

    const uri = recorder.uri;
    if (!uri) {
      stopping.current = false;
      setState({ kind: "error", message: t("voice.recordError") });
      return;
    }

    const outcome = await uploadVoiceNote(client, conversationId, { uri, name: "sesli-not.m4a", type: "audio/m4a" });
    stopping.current = false;
    if (outcome.ok) {
      setState({ kind: "idle" });
      onDraft(outcome.data);
    } else {
      setState({ kind: "error", message: outcome.unreachable ? t("communication.unreachable") : outcome.message ?? t("voice.error") });
    }
  }, [client, conversationId, onDraft, recorder]);

  const start = useCallback(async () => {
    const permission = await requestRecordingPermissionsAsync();
    if (!permission.granted) {
      setState({ kind: "error", message: t("voice.permissionDenied") });
      return;
    }

    try {
      await setAudioModeAsync({ allowsRecording: true, playsInSilentMode: true });
      await recorder.prepareToRecordAsync();
      recorder.record();
    } catch {
      setState({ kind: "error", message: t("voice.recordError") });
      return;
    }

    let seconds = 0;
    setState({ kind: "recording", seconds });
    timer.current = setInterval(() => {
      seconds += 1;
      setState({ kind: "recording", seconds: Math.min(seconds, VOICE_NOTE_MAX_SECONDS) });
      if (seconds >= VOICE_NOTE_MAX_SECONDS) {
        void stop();
      }
    }, 1000);
  }, [recorder, stop]);

  return { state, start, stop };
}
