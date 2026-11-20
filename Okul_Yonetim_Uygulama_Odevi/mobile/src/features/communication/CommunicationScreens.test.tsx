import { fireEvent, render, screen, within } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";

// Spec 0011 AC-13 (mobil, veli ve öğretmen): menüler yalnızca ilgili rolde ve okunmamış sayılarıyla; duyuru akışı (yükleniyor /
// boş / dolu / hata + tekrar dene, "yeni" işareti, okul bildirimi, "Daha fazla"), ayrıntı ve onay düğmeleri (mevcut cevap ve
// kim verdi, süresi dolmuşta kapalı, sunucu hatası); mesajlar listesi (okunmamış etiketi), konuşma (alan hatasında istek yok,
// gönderirken kilit, mesai dışı notu, gönderilemez durumu, "Daha eski") ve yeni konuşma — iki temada. Listeler kayar.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const baseMe = { fullName: "Ayla Ak", email: "ayla@ornek.test", tenant: tenantA, branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0 };

const announcement = { kind: "Announcement", id: "a1", title: "Müze gezisi", authorName: "Elif Öğretmen", childNames: ["Can Ak"], at: "2026-11-20T09:05:00Z", isRead: false, requiresConsent: true };
const schoolNotice = { kind: "SchoolNotice", id: "n1", title: "Can Ak bugün (20.11.2026) yoklamada gelmedi.", authorName: null, childNames: ["Can Ak"], at: "2026-11-20T08:00:00Z", isRead: true, requiresConsent: false };
const older = { kind: "Announcement", id: "a0", title: "Veli toplantısı", authorName: "Müdür", childNames: [], at: "2026-11-19T09:00:00Z", isRead: true, requiresConsent: false };
const detail = (consentOpen: boolean, answer: string | null = null) => ({
  id: "a1", title: "Müze gezisi", body: "Pazartesi müze gezisi var.", scope: "Classroom", authorName: "Elif Öğretmen", publishedAt: "2026-11-20T09:05:00Z",
  consentQuestion: "Geziye katılsın mı?", consentDeadline: "2026-11-27", consentOpen,
  children: [{ studentId: "c1", studentName: "Can Ak", answer, answeredByName: answer ? "Baba Ak" : null, answeredAt: answer ? "2026-11-20T09:10:00Z" : null }],
});
const conversation = { id: "k1", studentId: "c1", studentName: "Can Ak", counterpartUserId: "t1", counterpartName: "Elif Öğretmen", myRole: "Guardian", lastMessageAt: "2026-11-20T09:00:00Z", unreadCount: 3 };
const message = (id: string, body: string, mine = false) => ({ id, mine, body, sentAt: "2026-11-20T09:00:00Z" });

function json(status: number, body: unknown): Response {
  return new Response(body === null ? null : JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

let bodies: { path: string; body: unknown }[] = [];

function fakeApi(routes: Record<string, Handler>, options: { roles?: string[]; unreadAnnouncements?: number; unreadMessages?: number } = {}) {
  bodies = [];
  const requests: string[] = [];
  globalThis.fetch = (async (request: Request) => {
    const url = new URL(request.url);
    const path = url.pathname;
    requests.push(`${request.method} ${path}${url.search}`);
    if (request.method !== "GET" && !path.startsWith("/api/auth/")) {
      const text = await request.clone().text();
      bodies.push({ path, body: text ? JSON.parse(text) : null });
    }

    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...baseMe, roles: options.roles ?? ["Guardian"], unreadAnnouncementCount: options.unreadAnnouncements ?? 0, unreadMessageCount: options.unreadMessages ?? 0 });
    }

    const handler = routes[`${request.method} ${path}`];
    return handler ? handler(request) : json(404, {});
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

describe.each(["light", "dark"] as const)("Duyurular ve mesajlar — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("veli: 'Duyurular (2)' → akış (yeni işareti, okul bildirimi, kayan liste) → ayrıntı ve onay", async () => {
    fakeApi(
      {
        "GET /api/announcements/feed": () => json(200, { items: [announcement, schoolNotice], page: 1, hasMore: false, unreadCount: 1 }),
        "GET /api/announcements/a1": () => json(200, detail(true)),
        "POST /api/announcements/a1/consent": () => json(200, detail(true, "Approved").children[0]),
      },
      { unreadAnnouncements: 2, unreadMessages: 3 },
    );
    await signIn();
    expect(screen.getByRole("button", { name: "Mesajlar (3)" })).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Duyurular (2)" }));

    const rows = await screen.findAllByTestId("feed-row");
    const list = screen.getByTestId("feed-list");
    expect(list.type).toBe("RCTScrollView"); // conventions T16: liste kayar, "Geri" dışında
    expect(within(list).getAllByTestId("feed-row")).toHaveLength(2);
    expect(within(list).queryByRole("button", { name: "Hoş geldin'e dön" })).toBeNull();
    expect(rows[0].props.accessibilityLabel).toBe("okunmadı, Onay formu, Müze gezisi, Elif Öğretmen · Can Ak · 20.11.2026");
    expect(screen.getByLabelText("okunmadı")).toHaveTextContent("Yeni");
    expect(rows[1].props.accessibilityLabel).toContain("Okul bildirimi");

    await fireEvent.press(rows[0]);
    expect(await screen.findByText("Pazartesi müze gezisi var.")).toBeTruthy();
    expect(screen.getByText("Henüz cevap verilmedi")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Can Ak için onaylıyorum" }));
    expect(await screen.findByText("Cevabınız kaydedildi.")).toBeTruthy();
    expect(screen.getByText("Onaylandı — Baba Ak")).toBeTruthy();
    expect(bodies).toEqual([{ path: "/api/announcements/a1/consent", body: { studentId: "c1", answer: "Approved" } }]);
  });

  it("veli: mesajlar listesi (okunmamış etiketi) → konuşma → gönder, mesai dışı notu", async () => {
    let release: () => void = () => undefined;
    const sent = new Promise<Response>((resolve) => {
      release = () => resolve(json(200, { message: message("m2", "Can yarın gelmeyecek", true), outsideWorkingHours: true }));
    });
    fakeApi({
      "GET /api/conversations": () => json(200, [conversation]),
      "GET /api/conversations/k1/messages": () => json(200, { conversation: { ...conversation, unreadCount: 0 }, canSend: true, messages: [message("m1", "Merhaba Ayla Hanım")], hasMore: false }),
      "POST /api/conversations/k1/messages": () => sent,
    }, { unreadMessages: 3 });
    await signIn();
    await fireEvent.press(screen.getByRole("button", { name: "Mesajlar (3)" }));

    const row = await screen.findByTestId("conversation-row");
    expect(screen.getByTestId("conversations-list").type).toBe("RCTScrollView");
    expect(row.props.accessibilityLabel).toBe("Elif Öğretmen, Can Ak, 3 okunmamış mesaj");
    expect(within(screen.getByTestId("conversations-list")).queryByRole("button", { name: "Yeni mesaj" })).toBeNull();
    await fireEvent.press(row);

    expect(await screen.findByText("Merhaba Ayla Hanım")).toBeTruthy();
    expect(screen.getByTestId("messages-list").type).toBe("RCTScrollView");
    await fireEvent.changeText(screen.getByLabelText("Mesajınız"), "Can yarın gelmeyecek");
    await fireEvent.press(screen.getByRole("button", { name: "Gönder" }));
    const sending = await screen.findByRole("button", { name: "Gönderiliyor…" });
    expect(sending.props.accessibilityState).toMatchObject({ disabled: true, busy: true });
    release();
    expect(await screen.findByText("Öğretmen şu an mesai saatleri dışında; mesajınız iletildi.")).toBeTruthy();
    expect(screen.getByText("Can yarın gelmeyecek")).toBeTruthy();
    expect(bodies).toEqual([{ path: "/api/conversations/k1/messages", body: { body: "Can yarın gelmeyecek" } }]);
  });
});

it("akış: yükleniyor → hata + tekrar dene → boş", async () => {
  let calls = 0;
  let fail: () => void = () => undefined;
  const failed = new Promise<Response>((resolve) => { fail = () => resolve(json(500, {})); });
  fakeApi({
    "GET /api/announcements/feed": () =>
      ++calls === 1 ? failed : json(200, { items: [], page: 1, hasMore: false, unreadCount: 0 }),
  });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Duyurular" }));

  expect(await screen.findByText("Duyurular yükleniyor…")).toBeTruthy();
  fail();
  expect(await screen.findByRole("alert")).toHaveTextContent("Duyurular alınamadı.");
  await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
  expect(await screen.findByText("Henüz duyuru yok")).toBeTruthy();
});

it("akış: 'Daha fazla' sonraki sayfayı ekler", async () => {
  const requests = fakeApi({
    "GET /api/announcements/feed": (request) =>
      new URL(request.url).searchParams.get("page") === "2"
        ? json(200, { items: [older], page: 2, hasMore: false, unreadCount: 0 })
        : json(200, { items: [{ ...announcement, isRead: true }], page: 1, hasMore: true, unreadCount: 0 }),
  });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Duyurular" }));
  await fireEvent.press(await screen.findByRole("button", { name: "Daha fazla" }));

  expect(await screen.findByText("Veli toplantısı")).toBeTruthy();
  expect(screen.getAllByTestId("feed-row")).toHaveLength(2);
  expect(screen.queryByRole("button", { name: "Daha fazla" })).toBeNull();
  expect(requests).toContain("GET /api/announcements/feed?page=2");
});

it("onay süresi dolmuş: düğmeler kapalı, 'Süre doldu'; sunucu reddi duyurulur", async () => {
  fakeApi({
    "GET /api/announcements/feed": () => json(200, { items: [announcement], page: 1, hasMore: false, unreadCount: 1 }),
    "GET /api/announcements/a1": () => json(200, detail(false, "Declined")),
  });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Duyurular" }));
  await fireEvent.press(await screen.findByTestId("feed-row"));

  expect(await screen.findByText("Süre doldu")).toBeTruthy();
  expect(screen.getByText("Onaylanmadı — Baba Ak")).toBeTruthy();
  expect(screen.getByRole("button", { name: "Can Ak için onaylıyorum" }).props.accessibilityState).toMatchObject({ disabled: true });
  expect(screen.getByRole("button", { name: "Can Ak için onaylamıyorum" }).props.accessibilityState).toMatchObject({ disabled: true, selected: true });
});

it("onay: sunucu 'Onay süresi doldu.' derse duyurulur", async () => {
  fakeApi({
    "GET /api/announcements/feed": () => json(200, { items: [announcement], page: 1, hasMore: false, unreadCount: 1 }),
    "GET /api/announcements/a1": () => json(200, detail(true)),
    "POST /api/announcements/a1/consent": () => json(400, { detail: "Onay süresi doldu." }),
  });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Duyurular" }));
  await fireEvent.press(await screen.findByTestId("feed-row"));
  await fireEvent.press(await screen.findByRole("button", { name: "Can Ak için onaylamıyorum" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Onay süresi doldu.");
});

it("konuşma: boş mesajda alan hatası, istek gitmez; ilişki bitince gönderilemez; 'Daha eski'", async () => {
  const requests = fakeApi({
    "GET /api/conversations": () => json(200, [conversation]),
    "GET /api/conversations/k1/messages": (request) =>
      new URL(request.url).searchParams.get("before") === "m5"
        ? json(200, { conversation, canSend: true, messages: [message("m4", "Eski mesaj")], hasMore: false })
        : json(200, { conversation, canSend: true, messages: [message("m5", "Son mesaj")], hasMore: true }),
  });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Mesajlar" }));
  await fireEvent.press(await screen.findByTestId("conversation-row"));
  await screen.findByText("Son mesaj");

  await fireEvent.press(screen.getByRole("button", { name: "Gönder" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("Mesaj boş olamaz");
  expect(requests.some((r) => r.startsWith("POST /api/conversations"))).toBe(false);

  await fireEvent.press(screen.getByRole("button", { name: "Daha eski" }));
  expect(await screen.findByText("Eski mesaj")).toBeTruthy();
  expect(screen.getAllByTestId("message").map((m) => m.props.accessibilityLabel)).toEqual(["Elif Öğretmen: Eski mesaj", "Elif Öğretmen: Son mesaj"]);
  expect(requests).toContain("GET /api/conversations/k1/messages?before=m5");
});

it("konuşma: ilişki bittiyse alan ve düğme kapalı, açıklama görünür", async () => {
  fakeApi({
    "GET /api/conversations": () => json(200, [conversation]),
    "GET /api/conversations/k1/messages": () => json(200, { conversation, canSend: false, messages: [message("m1", "Eski konuşma")], hasMore: false }),
  });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Mesajlar" }));
  await fireEvent.press(await screen.findByTestId("conversation-row"));

  expect(await screen.findByText("Eski konuşma")).toBeTruthy();
  expect(screen.getByRole("alert")).toHaveTextContent("Bu konuşmaya artık mesaj gönderilemez.");
  expect(screen.getByLabelText("Mesajınız").props.editable).toBe(false);
  expect(screen.getByRole("button", { name: "Gönder" }).props.accessibilityState).toMatchObject({ disabled: true });
});

it("konuşma: sunucu hatası duyurulur", async () => {
  fakeApi({
    "GET /api/conversations": () => json(200, [conversation]),
    "GET /api/conversations/k1/messages": () => json(200, { conversation, canSend: true, messages: [], hasMore: false }),
    "POST /api/conversations/k1/messages": () => json(400, { detail: "Bu konuşmaya artık mesaj gönderilemez." }),
  });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Mesajlar" }));
  await fireEvent.press(await screen.findByTestId("conversation-row"));
  await fireEvent.changeText(await screen.findByLabelText("Mesajınız"), "Merhaba");
  await fireEvent.press(screen.getByRole("button", { name: "Gönder" }));

  expect(await screen.findByRole("alert")).toHaveTextContent("Bu konuşmaya artık mesaj gönderilemez.");
});

it("mesajlar boşken 'Henüz mesaj yok'; öğretmen yeni mesaj: sınıf → öğrenci → veli → konuşma", async () => {
  fakeApi(
    {
      "GET /api/conversations": () => json(200, []),
      "GET /api/conversations/contacts": () =>
        json(200, {
          asGuardian: [],
          asTeacher: [{ classroomId: "s1", classroomName: "5-A", students: [{ studentId: "c1", studentName: "Can Ak", guardians: [{ userId: "g1", fullName: "Ayla Ak" }] }] }],
        }),
      "POST /api/conversations": () => json(200, { ...conversation, counterpartUserId: "g1", counterpartName: "Ayla Ak", myRole: "Teacher", unreadCount: 0 }),
      "GET /api/conversations/k1/messages": () => json(200, { conversation: { ...conversation, counterpartName: "Ayla Ak" }, canSend: true, messages: [], hasMore: false }),
    },
    { roles: ["Teacher"] },
  );
  await signIn();
  expect(screen.queryByRole("button", { name: /Duyurular/ })).toBeNull();
  await fireEvent.press(screen.getByRole("button", { name: "Mesajlar" }));
  expect(await screen.findByText("Henüz mesaj yok")).toBeTruthy();

  await fireEvent.press(screen.getByRole("button", { name: "Yeni mesaj" }));
  await fireEvent.press(await screen.findByRole("button", { name: "5-A" }));
  await fireEvent.press(screen.getByRole("button", { name: "Can Ak" }));
  await fireEvent.press(screen.getByRole("button", { name: "Ayla Ak" }));

  expect(await screen.findByRole("header", { name: "Ayla Ak" })).toBeTruthy();
  expect(bodies).toEqual([{ path: "/api/conversations", body: { studentId: "c1", counterpartUserId: "g1" } }]);
});

it("veli yeni mesaj: çocuk → öğretmen", async () => {
  fakeApi({
    "GET /api/conversations": () => json(200, []),
    "GET /api/conversations/contacts": () => json(200, { asGuardian: [{ studentId: "c1", studentName: "Can Ak", teachers: [{ userId: "t1", fullName: "Elif Öğretmen" }] }], asTeacher: [] }),
    "POST /api/conversations": () => json(200, conversation),
    "GET /api/conversations/k1/messages": () => json(200, { conversation, canSend: true, messages: [], hasMore: false }),
  });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Mesajlar" }));
  await fireEvent.press(await screen.findByRole("button", { name: "Yeni mesaj" }));
  await fireEvent.press(await screen.findByRole("button", { name: "Can Ak" }));
  await fireEvent.press(screen.getByRole("button", { name: "Elif Öğretmen" }));

  expect(await screen.findByRole("header", { name: "Elif Öğretmen" })).toBeTruthy();
  expect(bodies).toEqual([{ path: "/api/conversations", body: { studentId: "c1", counterpartUserId: "t1" } }]);
});

it("menüler yalnızca ilgili rolde: muhasebede Duyurular, Mesajlar, Mesai saatlerim yok", async () => {
  const requests = fakeApi({}, { roles: ["Accounting"] });
  await signIn();

  expect(screen.queryByRole("button", { name: /Duyurular/ })).toBeNull();
  expect(screen.queryByRole("button", { name: /Mesajlar/ })).toBeNull();
  expect(screen.queryByRole("button", { name: "Mesai saatlerim" })).toBeNull();
  expect(requests.some((r) => r.includes("/api/announcements") || r.includes("/api/conversations"))).toBe(false);
});

it("velide 'Mesai saatlerim' yok", async () => {
  fakeApi({});
  await signIn();

  expect(screen.getByRole("button", { name: "Duyurular" })).toBeTruthy();
  expect(screen.getByRole("button", { name: "Mesajlar" })).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Mesai saatlerim" })).toBeNull();
});
