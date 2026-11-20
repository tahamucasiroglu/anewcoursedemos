// Sesli nottan veli mesajı (spec 0012): ses dosyasını çok parçalı form olarak yükler (oturum anahtarı ve 401'de tek yenileme
// AuthSession'ın istemcisinden), "Vazgeç" ile taslağı siler. Taslak veliye gitmez; gönderim konuşmanın mesaj ucundan
// aiDraftId ile yapılır (öğretmenin "Gönder"i onaydır — BR-5).
import { callApi, type ApiOutcome } from "../apiOutcome";
import type { AuthSession } from "../auth/session";
import type { components } from "../schema";

export type VoiceMessageDraft = components["schemas"]["VoiceMessageDraftDto"];

/** İstemcinin kaydı durdurduğu üst sınır (sunucu da ses servisinin ölçtüğü süreye bakar). */
export const VOICE_NOTE_MAX_SECONDS = 60;
export const VOICE_NOTE_MAX_BYTES = 2 * 1024 * 1024;

/** React Native dosya parçası (kaydın yerel adresi) ya da tarayıcıdaki Blob. */
export type VoiceNoteFile = { uri: string; name: string; type: string } | Blob;

export function uploadVoiceNote(client: AuthSession["client"], conversationId: string, file: VoiceNoteFile): Promise<ApiOutcome<VoiceMessageDraft>> {
  const form = new FormData();
  form.append("conversationId", conversationId);
  if (file instanceof Blob) {
    form.append("audio", file, "sesli-not.webm");
  } else {
    // React Native FormData dosyayı { uri, name, type } nesnesiyle alır; tip tanımı Blob ister.
    form.append("audio", file as unknown as Blob);
  }

  return callApi(
    client.POST("/api/ai/voice-message-drafts", {
      body: {} as never,
      bodySerializer: () => form,
    }),
  );
}

export function discardVoiceDraft(client: AuthSession["client"], draftId: string): Promise<ApiOutcome<unknown>> {
  return callApi(client.DELETE("/api/ai/drafts/{id}", { params: { path: { id: draftId } } }));
}
