import { fireEvent, render, screen, within } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0010 AC-9 ve AC-13 (mobil, veli bildirimleri): "Bildirimler (2)" yalnızca velide; liste sunucunun sırasıyla (en
// yenisi üstte), okunmamış "Yeni", düzeltilen "Düzeltildi" etiketiyle; liste gelince "okundu" isteği gider ve geri dönünce
// sayı sıfırlanır; boş liste "Henüz bildirim yok"; hata + tekrar dene — iki temada.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const guardianMe = { fullName: "Ayla Ak", email: "ayla@ornek.test", tenant: tenantA, roles: ["Guardian"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false };
const absent = { id: "n1", studentId: "c2", studentName: "Can Ak", day: "2026-11-20", kind: "Absent", text: "Can Ak bugün (20.11.2026) A Koleji'nda yoklamada gelmedi olarak işaretlendi. Bilginiz yoksa okulla iletişime geçin.", isRead: false, at: "2026-11-20T09:05:00Z" };
const corrected = { id: "n2", studentId: "c1", studentName: "Zeynep Ak", day: "2026-11-20", kind: "Corrected", text: "Zeynep Ak için bugünkü yoklama düzeltildi: okulda.", isRead: true, at: "2026-11-20T09:00:00Z" };

function json(status: number, body: unknown): Response {
  return new Response(body === null ? null : JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

function fakeApi(list: Handler, options: { roles?: string[]; unread?: number[] } = {}) {
  const requests: string[] = [];
  const unread = [...(options.unread ?? [0])];
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    requests.push(`${request.method} ${path}`);
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...guardianMe, roles: options.roles ?? guardianMe.roles, unreadNoticeCount: unread.length > 1 ? unread.shift() : unread[0] });
    }

    if (path === "/api/notices" && request.method === "GET") {
      return list(request);
    }

    if (path === "/api/notices/read") {
      return new Response(null, { status: 204 });
    }

    return json(404, {});
  }) as typeof fetch;
  return requests;
}

async function signIn() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "ayla@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  await screen.findByTestId("welcome");
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Bildirimler — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("'Bildirimler (2)' → liste etiketlerle, okundu isteği; dönünce sayı sıfır", async () => {
    const requests = fakeApi(() => json(200, [absent, corrected]), { unread: [2, 0] });
    await signIn();
    await fireEvent.press(screen.getByRole("button", { name: "Bildirimler (2)" }));

    expect(await screen.findByRole("header", { name: "Bildirimler" })).toBeTruthy();
    const rows = await screen.findAllByTestId("notice-row");
    const list = screen.getByTestId("notices-list");
    expect(list.type).toBe("RCTScrollView"); // 0010 review F-2: liste kayar, "Geri" dışında
    expect(within(list).getAllByTestId("notice-row")).toHaveLength(rows.length);
    expect(within(list).queryByRole("button", { name: "Hoş geldin'e dön" })).toBeNull();
    expect(rows.map((r) => r.props.accessibilityLabel)).toEqual([
      `Yeni, 20.11.2026, ${absent.text}`,
      `Düzeltildi, 20.11.2026, ${corrected.text}`,
    ]);
    expect(requests).toContain("POST /api/notices/read");

    await fireEvent.press(screen.getByRole("button", { name: "Hoş geldin'e dön" }));
    expect(await screen.findByRole("button", { name: "Bildirimler" })).toBeTruthy();
  });
});

it("bildirim yoksa 'Henüz bildirim yok', okundu isteği gitmez", async () => {
  const requests = fakeApi(() => json(200, []));
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Bildirimler" }));

  expect(await screen.findByText("Henüz bildirim yok")).toBeTruthy();
  expect(requests).not.toContain("POST /api/notices/read");
});

it("yükleniyor → hata + tekrar dene → liste", async () => {
  let calls = 0;
  let fail: () => void = () => undefined;
  fakeApi(() => (++calls === 1 ? new Promise<Response>((resolve) => { fail = () => resolve(json(500, {})); }) : json(200, [corrected])));
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Bildirimler" }));

  expect(await screen.findByText("Bildirimler yükleniyor…")).toBeTruthy();
  fail();
  expect(await screen.findByRole("alert")).toHaveTextContent("Bildirimler alınamadı.");
  await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText(corrected.text)).toBeTruthy();
});

it("veli olmayan kişide 'Bildirimler' yok", async () => {
  const requests = fakeApi(() => json(200, []), { roles: ["Teacher"] });
  await signIn();

  expect(screen.queryByRole("button", { name: /Bildirimler/ })).toBeNull();
  expect(requests.some((r) => r.includes("/api/notices"))).toBe(false);
});
