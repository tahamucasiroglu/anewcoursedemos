import * as ExpoAudio from "expo-audio";
import { fireEvent, render, screen, within } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0012 AC-12 (mobil konuşma ekranı, iki tema): "Sesli not" yalnızca öğretmende ve ilişki sürerken; kaydediliyor
// (sayaç, Durdur) → dönüştürülüyor (gönder kilitli) → taslak hazır ("AI taslağı — göndermeden önce kontrol edin", açılır
// kaynak döküm, düzenlenebilir kutu, Vazgeç); "Gönder" taslak kimliğiyle ve öğretmenin son metniyle gider; Vazgeç taslağı
// siler; sunucu hataları gösterilir ve elle gönderim çalışır; velinin konuşma ekranında düğme yok.
type Handler = (request: Request) => Promise<Response> | Response;
type AudioMock = { reset: () => void; recorded: number; stopped: number; permission: boolean };

const audio = (ExpoAudio as unknown as { __audio: AudioMock }).__audio;
const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const baseMe = { fullName: "Elif Öğretmen", email: "elif@ornek.test", tenant: tenantA, branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0, unreadAnnouncementCount: 0, unreadMessageCount: 0 };
const teacherConversation = { id: "k1", studentId: "c1", studentName: "Can Ak", counterpartUserId: "g1", counterpartName: "Ayla Ak", myRole: "Teacher", lastMessageAt: "2026-11-20T09:00:00Z", unreadCount: 0 };
const guardianConversation = { ...teacherConversation, counterpartUserId: "t1", counterpartName: "Elif Öğretmen", myRole: "Guardian" };
const draft = { draftId: "d1", transcript: "can bugün ödevini getirmedi", draft: "Sayın Veli, Can bugün ödevini getirmedi.", durationSeconds: 12.5 };

function json(status: number, body: unknown): Response {
  return new Response(body === null ? null : JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

let sentBodies: unknown[] = [];

function fakeApi(routes: Record<string, Handler>, roles: string[] = ["Teacher"]) {
  sentBodies = [];
  const requests: string[] = [];
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    requests.push(`${request.method} ${path}`);
    if (request.method === "POST" && path.endsWith("/messages")) {
      sentBodies.push(JSON.parse(await request.clone().text()));
    }

    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...baseMe, roles });
    }

    const handler = routes[`${request.method} ${path}`];
    return handler ? handler(request) : json(404, {});
  }) as typeof fetch;
  return requests;
}

function conversationRoutes(conversation: typeof teacherConversation, canSend = true): Record<string, Handler> {
  return {
    "GET /api/conversations": () => json(200, [conversation]),
    "GET /api/conversations/k1/messages": () => json(200, { conversation, canSend, messages: [], hasMore: false }),
  };
}

async function openConversation() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "elif@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  await screen.findByTestId("welcome");
  await fireEvent.press(screen.getByRole("button", { name: "Mesajlar" }));
  await fireEvent.press(await screen.findByTestId("conversation-row"));
  await screen.findByRole("header", { name: /Ayla Ak|Elif Öğretmen/ });
}

function sendButton() {
  return screen.getByRole("button", { name: /Gönder/ });
}

beforeEach(async () => {
  reset();
  audio.reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Sesli not — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("öğretmen: kayıt → dönüştürülüyor (gönder kilitli) → taslak paneli → düzenleyip gönderir (taslak kimliğiyle)", async () => {
    let release: (response: Response) => void = () => {};
    const upload = new Promise<Response>((resolve) => {
      release = resolve;
    });
    const requests = fakeApi({
      ...conversationRoutes(teacherConversation),
      "POST /api/ai/voice-message-drafts": () => upload,
      "POST /api/conversations/k1/messages": () => json(200, { message: { id: "m1", mine: true, body: "Sayın Veli, Can bugün ödevini getirmedi. Yarın getirsin.", sentAt: "2026-11-20T09:05:00Z" }, outsideWorkingHours: false }),
    });
    await openConversation();

    await fireEvent.press(screen.getByRole("button", { name: "Sesli not" }));
    const recording = await screen.findByTestId("voice-recording");
    expect(within(recording).getByText("Kaydediliyor… 0 sn").props.accessibilityLiveRegion).toBe("polite");
    expect(sendButton().props.accessibilityState).toMatchObject({ disabled: true });
    expect(audio.recorded).toBe(1);

    await fireEvent.press(screen.getByRole("button", { name: "Durdur" }));
    expect(await screen.findByText("Dönüştürülüyor…")).toBeTruthy();
    expect(sendButton().props.accessibilityState).toMatchObject({ disabled: true });
    expect(requests).toContain("POST /api/ai/voice-message-drafts");
    expect(audio.stopped).toBe(1);

    release(json(200, draft));
    const panel = await screen.findByTestId("ai-draft");
    expect(within(panel).getByText("AI taslağı — göndermeden önce kontrol edin")).toBeTruthy();
    expect(screen.getByLabelText("Mesajınız").props.value).toBe(draft.draft);
    expect(screen.queryByText(draft.transcript)).toBeNull();
    await fireEvent.press(within(panel).getByRole("button", { name: "Kaynak: sesli notun dökümü" }));
    expect(screen.getByTestId("ai-draft-source")).toHaveTextContent(draft.transcript);
    expect(screen.queryByRole("button", { name: "Sesli not" })).toBeNull();
    expect(sendButton().props.accessibilityState).toMatchObject({ disabled: false });

    await fireEvent.changeText(screen.getByLabelText("Mesajınız"), "Sayın Veli, Can bugün ödevini getirmedi. Yarın getirsin.");
    await fireEvent.press(sendButton());

    expect(await screen.findByText("Sayın Veli, Can bugün ödevini getirmedi. Yarın getirsin.", { exact: true })).toBeTruthy();
    expect(sentBodies).toEqual([{ body: "Sayın Veli, Can bugün ödevini getirmedi. Yarın getirsin.", aiDraftId: "d1" }]);
    expect(screen.queryByTestId("ai-draft")).toBeNull();
    expect(screen.getByRole("button", { name: "Sesli not" })).toBeTruthy();
  });

  it("Vazgeç: taslak silinir (DELETE), kutu boşalır, Sesli not geri gelir", async () => {
    const requests = fakeApi({
      ...conversationRoutes(teacherConversation),
      "POST /api/ai/voice-message-drafts": () => json(200, draft),
      "DELETE /api/ai/drafts/d1": () => new Response(null, { status: 204 }),
    });
    await openConversation();
    await fireEvent.press(screen.getByRole("button", { name: "Sesli not" }));
    await fireEvent.press(await screen.findByRole("button", { name: "Durdur" }));
    await screen.findByTestId("ai-draft");

    await fireEvent.press(screen.getByRole("button", { name: "Vazgeç" }));

    expect(await screen.findByRole("button", { name: "Sesli not" })).toBeTruthy();
    expect(requests).toContain("DELETE /api/ai/drafts/d1");
    expect(screen.queryByTestId("ai-draft")).toBeNull();
    expect(screen.getByLabelText("Mesajınız").props.value).toBe("");
    expect(sentBodies).toEqual([]);
  });

  it("veli: konuşma ekranında Sesli not düğmesi yok", async () => {
    fakeApi(conversationRoutes(guardianConversation), ["Guardian"]);
    await openConversation();

    expect(screen.getByLabelText("Mesajınız")).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Sesli not" })).toBeNull();
  });
});

it("ilişkisi biten konuşmada öğretmende de Sesli not yok", async () => {
  fakeApi(conversationRoutes(teacherConversation, false));
  await openConversation();

  expect(screen.getByText("Bu konuşmaya artık mesaj gönderilemez.")).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Sesli not" })).toBeNull();
});

it.each([
  "Yapay zekâ şu an kullanılamıyor. Mesajı elle yazabilirsiniz.",
  "Ses anlaşılamadı, lütfen tekrar deneyin.",
  "Sesli not en fazla 60 saniye olabilir.",
])("sunucu hatası duyurulur ve elle gönderim çalışır: %s", async (detail) => {
  fakeApi({
    ...conversationRoutes(teacherConversation),
    "POST /api/ai/voice-message-drafts": () => json(400, { detail }),
    "POST /api/conversations/k1/messages": () => json(200, { message: { id: "m1", mine: true, body: "Elle yazdım", sentAt: "2026-11-20T09:05:00Z" }, outsideWorkingHours: false }),
  });
  await openConversation();
  await fireEvent.press(screen.getByRole("button", { name: "Sesli not" }));
  await fireEvent.press(await screen.findByRole("button", { name: "Durdur" }));

  const alert = await screen.findByRole("alert");
  expect(alert).toHaveTextContent(detail);
  expect(alert.props.accessibilityLiveRegion).toBe("assertive");
  expect(screen.queryByTestId("ai-draft")).toBeNull();

  await fireEvent.changeText(screen.getByLabelText("Mesajınız"), "Elle yazdım");
  await fireEvent.press(sendButton());
  expect(await screen.findByText("Elle yazdım")).toBeTruthy();
  expect(sentBodies).toEqual([{ body: "Elle yazdım" }]);
});
