import * as ExpoAudio from "expo-audio";
import { act, fireEvent, render, screen } from "@testing-library/react-native";
import { createOkulApiClient } from "@okul/api-client";
import { ThemeProvider } from "../../theme/ThemeProvider";
import { VoiceNoteButton } from "./VoiceNoteButton";

// Spec 0012 AC-12 (düğme): erişilebilirlik etiketleri; kayıt sayacı saniye saniye artar ve 60. saniyede kayıt kendiliğinden
// durup ses yüklenir; mikrofon izni yoksa "Mikrofon izni verilmedi." ve kayıt/yükleme yok; kayıt başlatılamazsa hata;
// kayıt ve dönüştürme sürerken "meşgul" bildirilir (gönder kilidi).
type AudioMock = { reset: () => void; recorded: number; stopped: number; permission: boolean; failPrepare: boolean };

const audio = (ExpoAudio as unknown as { __audio: AudioMock }).__audio;
const originalFetch = globalThis.fetch;
let uploads = 0;

function client() {
  globalThis.fetch = (async (request: Request) => {
    if (new URL(request.url).pathname === "/api/ai/voice-message-drafts") {
      uploads += 1;
      return new Response(JSON.stringify({ draftId: "d1", transcript: "döküm", draft: "taslak", durationSeconds: 60 }), { status: 200, headers: { "Content-Type": "application/json" } });
    }

    return new Response(null, { status: 404 });
  }) as typeof fetch;
  return createOkulApiClient("http://api.test");
}

async function renderButton(onDraft = jest.fn(), onBusyChange = jest.fn()) {
  await render(
    <ThemeProvider>
      <VoiceNoteButton client={client()} conversationId="k1" onDraft={onDraft} onBusyChange={onBusyChange} />
    </ThemeProvider>,
  );
  return { onDraft, onBusyChange };
}

beforeEach(() => {
  audio.reset();
  uploads = 0;
});

afterEach(() => {
  jest.useRealTimers();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

it("Sesli not düğmesi etiketli; kayıtta Durdur etiketli ve sayaç ekran okuyucuya duyurulur", async () => {
  await renderButton();
  const button = await screen.findByRole("button", { name: "Sesli not" });
  expect(button.props.accessibilityHint).toBe("En fazla 60 saniye kaydeder; yazıya döküp mesaj taslağı hazırlar");

  await fireEvent.press(button);

  expect(await screen.findByRole("button", { name: "Durdur" })).toBeTruthy();
  expect(screen.getByText("Kaydediliyor… 0 sn").props.accessibilityLiveRegion).toBe("polite");
});

it("sayaç saniye saniye artar; 60. saniyede kayıt kendiliğinden durur ve ses yüklenir", async () => {
  jest.useFakeTimers();
  const { onDraft, onBusyChange } = await renderButton();
  await fireEvent.press(await screen.findByRole("button", { name: "Sesli not" }));
  await act(async () => {});
  expect(onBusyChange).toHaveBeenLastCalledWith(true);

  await act(async () => {
    jest.advanceTimersByTime(3000);
  });
  expect(screen.getByText("Kaydediliyor… 3 sn")).toBeTruthy();
  expect(audio.stopped).toBe(0);

  await act(async () => {
    jest.advanceTimersByTime(57_000);
  });
  jest.useRealTimers();

  expect(audio.stopped).toBe(1);
  await screen.findByRole("button", { name: "Sesli not" });
  expect(uploads).toBe(1);
  expect(onDraft).toHaveBeenCalledWith({ draftId: "d1", transcript: "döküm", draft: "taslak", durationSeconds: 60 });
  expect(onBusyChange).toHaveBeenLastCalledWith(false);
});

it("mikrofon izni yoksa 'Mikrofon izni verilmedi.'; kayıt ve yükleme yok", async () => {
  audio.permission = false;
  await renderButton();

  await fireEvent.press(await screen.findByRole("button", { name: "Sesli not" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Mikrofon izni verilmedi.");
  expect(audio.recorded).toBe(0);
  expect(uploads).toBe(0);
});

it("kayıt başlatılamazsa hata gösterilir", async () => {
  audio.failPrepare = true;
  await renderButton();

  await fireEvent.press(await screen.findByRole("button", { name: "Sesli not" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Ses kaydı başlatılamadı.");
  expect(uploads).toBe(0);
});
