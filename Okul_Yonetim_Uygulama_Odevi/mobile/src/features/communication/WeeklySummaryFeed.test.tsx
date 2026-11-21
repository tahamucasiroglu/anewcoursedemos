import { fireEvent, render, screen, within } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0014 AC-10 (mobil, veli): duyuru akışında haftalık gelişim özeti "Haftalık gelişim özeti — 16–22 Kasım" başlığı ve çocuk
// adıyla görünür ve dokunulabilir; ayrıntıda özet, "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı." etiketi ve dayanak satırı
// (kayan içerik), dönünce akış yenilenir; sunucu reddederse (başka velinin çocuğu — 404) hata + tekrar dene. İki tema.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const me = { fullName: "Ayla Ak", email: "ayla@ornek.test", tenant: tenantA, roles: ["Guardian"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0, unreadAnnouncementCount: 0, unreadMessageCount: 0, teachesClassroom: false };

const summaryItem = { kind: "WeeklySummary", id: "w1", title: "Haftalık gelişim özeti — 16–22 Kasım", authorName: null, childNames: ["Can Ak"], at: "2026-11-20T09:05:00Z", isRead: false, requiresConsent: false };
const schoolNotice = { kind: "SchoolNotice", id: "n1", title: "Can Ak bugün (20.11.2026) yoklamada gelmedi.", authorName: null, childNames: ["Can Ak"], at: "2026-11-20T08:00:00Z", isRead: true, requiresConsent: false };
const summaryText = "Can bu hafta dört gün okuldaydı ve iki ödevini teslim etti.\n\nYapay zekâ desteğiyle hazırlandı, öğretmen onayladı.\nDayanak: 4 gün geldi, 1 gün gelmedi; 2 ödev; 1 not";
const notice = { id: "w1", studentId: "c1", studentName: "Can Ak", day: "2026-11-16", kind: "WeeklySummary", text: summaryText, isRead: true, at: "2026-11-20T09:05:00Z", title: "Haftalık gelişim özeti — 16–22 Kasım" };

function json(status: number, body: unknown): Response {
  return new Response(body === null ? null : JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

function fakeApi(routes: Record<string, Handler>) {
  const requests: string[] = [];
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
      return json(200, me);
    }

    const handler = routes[`${request.method} ${path}`];
    return handler ? handler(request) : json(404, {});
  }) as typeof fetch;
  return requests;
}

async function openFeed() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "ayla@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  await screen.findByTestId("welcome");
  await fireEvent.press(screen.getByRole("button", { name: "Duyurular" }));
  return screen.findAllByTestId("feed-row");
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

describe.each(["light", "dark"] as const)("Haftalık gelişim özeti (veli) — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("akışta başlık ve çocuk adıyla → ayrıntıda özet, etiket ve dayanak satırı → akışa dönüş", async () => {
    const requests = fakeApi({
      "GET /api/announcements/feed": () => json(200, { items: [summaryItem, schoolNotice], page: 1, hasMore: false, unreadCount: 0 }),
      "GET /api/notices/w1": () => json(200, notice),
    });

    const rows = await openFeed();
    expect(rows[0].props.accessibilityLabel).toBe("okunmadı, Haftalık gelişim özeti — 16–22 Kasım, Okul bildirimi · Can Ak · 20.11.2026");
    expect(rows[0].props.accessibilityRole).toBe("button");
    expect(rows[1].props.accessibilityRole).toBeUndefined();
    await fireEvent.press(rows[0]);

    expect(await screen.findByRole("header", { name: "Haftalık gelişim özeti — 16–22 Kasım" })).toBeTruthy();
    const content = screen.getByTestId("weekly-summary-content");
    expect(content.type).toBe("RCTScrollView");
    expect(within(content).getByText("Can Ak · 20.11.2026")).toBeTruthy();
    const text = screen.getByTestId("weekly-summary-text");
    expect(text).toHaveTextContent(summaryText);
    expect(text.props.children).toContain("Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı.");
    expect(text.props.children).toContain("Dayanak: 4 gün geldi, 1 gün gelmedi; 2 ödev; 1 not");

    await fireEvent.press(screen.getByRole("button", { name: "Duyurulara dön" }));
    expect(await screen.findByTestId("feed")).toBeTruthy();
    expect(requests.filter((r) => r === "GET /api/announcements/feed")).toHaveLength(2);
  }, 15_000);

  it("sunucu reddederse (404) hata duyurulur, tekrar dene ayrıntıyı yeniden ister", async () => {
    let calls = 0;
    fakeApi({
      "GET /api/announcements/feed": () => json(200, { items: [summaryItem], page: 1, hasMore: false, unreadCount: 0 }),
      "GET /api/notices/w1": () => (++calls === 1 ? json(404, { detail: "Bildirim bulunamadı" }) : json(200, notice)),
    });

    await fireEvent.press((await openFeed())[0]);
    expect(await screen.findByRole("alert")).toHaveTextContent("Bildirim bulunamadı");
    await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
    expect(await screen.findByTestId("weekly-summary-text")).toHaveTextContent(summaryText);
  });
});
