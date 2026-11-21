import { fireEvent, render, screen } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";
import { warmUpFirstRender } from "../../test/warmUp";

// Spec 0016 AC-13 (veli ekranları, iki tema) ve AC-4/AC-10: "Randevular" yalnızca velide; Randevularım yükleniyor / boş ("Henüz randevunuz
// yok") / hata + tekrar dene / dolu — yaklaşan ve geçmiş, durum yazıyla ("Reddedildi" + "Neden: …", "Süresi doldu"), öteki velinin talebinde
// "Talep eden", iptal düğmesi yalnızca canCancel'da, iptal onay iletişimi → "Randevu iptal edildi." ve liste yenilenir; liste kayan öğede
// (T16). Randevu al: çocuk yoksa "Kayıtlı çocuğunuz yok", birden çok çocukta seçim, öğretmen yoksa "Bu sınıfa atanmış öğretmen yok", boş saat
// yoksa "Bu öğretmenin boş görüşme saati yok", saat düğmesi etiketi gün + saat + "boş", konu hatası istek göndermez, gönderiliyor-düğme
// kilitli, "Bu saat artık dolu." sonrası boş saatler yenilenir, başarıda "Talebiniz öğretmene iletildi".
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const guardianMe = { fullName: "Ayla Yılmaz", email: "ayla@ornek.test", tenant: tenantA, roles: ["Guardian"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0, pendingAppointmentCount: 0 };

const ali = { id: "c1", fullName: "Ali Yılmaz", branchName: "Merkez", classroomName: "5-A" };
const can = { id: "c2", fullName: "Can Yılmaz", branchName: "Avcılar", classroomName: "2-B" };
const teacher = { userId: "t1", fullName: "Selin Öğretmen" };
const slot1 = { slotId: "s1", startsAt: "2026-11-26T11:00:00Z", endsAt: "2026-11-26T11:20:00Z" };
const slot2 = { slotId: "s2", startsAt: "2026-11-26T11:20:00Z", endsAt: "2026-11-26T11:40:00Z" };

function row(overrides: Record<string, unknown>) {
  return { id: "a1", teacherUserId: "t1", teacherName: "Selin Öğretmen", studentId: "c1", studentName: "Ali Yılmaz", startsAt: "2026-11-26T11:00:00Z", endsAt: "2026-11-26T11:20:00Z", state: "Pending", reason: null, requestedByMe: true, requestedByName: "Ayla Yılmaz", canCancel: true, ...overrides };
}

const mine = {
  upcoming: [
    row({}),
    row({ id: "a2", state: "Rejected", reason: "Toplantım var", canCancel: false, startsAt: "2026-11-27T07:00:00Z" }),
    row({ id: "a3", state: "Approved", requestedByMe: false, requestedByName: "Murat Yılmaz", canCancel: false, startsAt: "2026-11-28T07:00:00Z" }),
  ],
  past: [row({ id: "a4", state: "Expired", canCancel: false, startsAt: "2026-11-10T07:00:00Z" })],
};

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

function business(message: string): Response {
  return new Response(JSON.stringify({ type: "https://example.com/probs/business", title: "Rule violation", status: 400, detail: message }), { status: 400, headers: { "Content-Type": "application/json" } });
}

interface Handlers {
  roles?: string[];
  mine?: Handler;
  children?: Handler;
  teachers?: Handler;
  slots?: Handler;
  request?: Handler;
  cancel?: Handler;
}

function fakeApi(handlers: Handlers) {
  const requests: { method: string; path: string; body: unknown }[] = [];
  globalThis.fetch = (async (request: Request) => {
    const url = new URL(request.url);
    const path = url.pathname;
    const text = request.method === "POST" ? await request.clone().text() : "";
    requests.push({ method: request.method, path: path + url.search, body: text ? JSON.parse(text) : null });
    const route: [RegExp, string, Handler | undefined][] = [
      [/^\/api\/appointments\/mine$/, "GET", handlers.mine],
      [/^\/api\/my\/children$/, "GET", handlers.children],
      [/^\/api\/appointments\/teachers$/, "GET", handlers.teachers],
      [/^\/api\/appointments\/teachers\/[^/]+\/slots$/, "GET", handlers.slots],
      [/^\/api\/appointments$/, "POST", handlers.request],
      [/^\/api\/appointments\/[^/]+\/cancel$/, "POST", handlers.cancel],
    ];
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...guardianMe, roles: handlers.roles ?? guardianMe.roles });
    }

    const match = route.find(([pattern, method, handler]) => pattern.test(path) && method === request.method && handler);
    return match ? match[2]!(request) : json(404, {});
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

async function openMine() {
  await fireEvent.press(screen.getByRole("button", { name: "Randevular" }));
}

beforeEach(async () => {
  reset();
  await AsyncStorage.clear();
});

afterAll(() => {
  globalThis.fetch = originalFetch;
});

// İlk çizimin soğuk başlangıç bedeli testlerden önce, doğrulamasız bir turda ödenir (conventions T21; mobile/src/test/warmUp.ts).
warmUpFirstRender(async () => {
  fakeApi({ mine: () => json(200, mine), children: () => json(200, [ali]), teachers: () => json(200, [teacher]), slots: () => json(200, [slot1]) });
  await signIn();
  await openMine();
  await screen.findAllByTestId("my-appointment");
  await fireEvent.press(screen.getByRole("button", { name: "Randevu al" }));
  await screen.findByTestId("book-appointment");
});

describe.each(["light", "dark"] as const)("Randevular (veli) — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("Randevularım yükleniyor → boş → (hata + tekrar dene) → dolu: durumlar yazıyla, neden, talep eden, iptal yalnızca canCancel'da", async () => {
    let release: () => void = () => undefined;
    const first = new Promise<Response>((resolve) => {
      release = () => resolve(json(200, { upcoming: [], past: [] }));
    });
    let calls = 0;
    fakeApi({ mine: () => (++calls === 1 ? first : calls === 2 ? json(500, { detail: "Beklenmeyen hata" }) : json(200, mine)) });
    await signIn();
    expect(screen.queryByText(/bekleyen/)).toBeNull();
    await openMine();
    expect(await screen.findByText("Yükleniyor…")).toBeTruthy();
    release();
    expect(await screen.findByText("Henüz randevunuz yok")).toBeTruthy();

    await fireEvent.press(screen.getByRole("button", { name: "Geri" }));
    await openMine();
    expect(await screen.findByText("Beklenmeyen hata")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByText("Yaklaşan")).toBeTruthy();
    expect(screen.getByText("Geçmiş (son 30 gün)")).toBeTruthy();
    expect(screen.getByTestId("my-appointments-list").type).toBe("RCTScrollView");
    expect(screen.getAllByTestId("my-appointment")).toHaveLength(4);
    expect(screen.getByText("Perşembe 26 Kasım 14:00")).toBeTruthy();
    expect(screen.getByText("Onay bekliyor")).toBeTruthy();
    expect(screen.getByText("Reddedildi")).toBeTruthy();
    expect(screen.getByText("Neden: Toplantım var")).toBeTruthy();
    expect(screen.getByText("Onaylandı")).toBeTruthy();
    expect(screen.getByText("Talep eden: Murat Yılmaz")).toBeTruthy();
    expect(screen.getByText("Süresi doldu")).toBeTruthy();
    expect(screen.getAllByText("Selin Öğretmen · Ali Yılmaz")).toHaveLength(4);
    expect(screen.getAllByRole("button", { name: "İptal et" })).toHaveLength(1);
  });

  it("iptal onay iletişimi: Vazgeç istek göndermez; Evet → işleniyor kilitli → 'Randevu iptal edildi.' ve liste yenilenir; hata duyurulur", async () => {
    let release: () => void = () => undefined;
    const cancelled = new Promise<Response>((resolve) => {
      release = () => resolve(json(200, { appointmentId: "a1", state: "CancelledByGuardian" }));
    });
    let cancels = 0;
    const requests = fakeApi({ mine: () => json(200, mine), cancel: () => (++cancels === 1 ? cancelled : business("Başlamış randevu iptal edilemez.")) });
    await signIn();
    await openMine();
    await fireEvent.press(await screen.findByRole("button", { name: "İptal et" }));
    expect(screen.getByText("Randevu iptal edilsin mi?")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Vazgeç" }));
    expect(requests.some((r) => r.path.endsWith("/cancel"))).toBe(false);

    await fireEvent.press(screen.getByRole("button", { name: "İptal et" }));
    await fireEvent.press(screen.getByRole("button", { name: "Evet, iptal et" }));
    expect(await screen.findByText("İptal ediliyor…")).toBeTruthy();
    expect(screen.getByRole("button", { name: "İptal ediliyor…" })).toBeDisabled();
    release();
    expect(await screen.findByText("Randevu iptal edildi.")).toBeTruthy();
    expect(requests.find((r) => r.path.endsWith("/cancel"))!.path).toBe("/api/appointments/a1/cancel");
    expect(requests.filter((r) => r.path === "/api/appointments/mine")).toHaveLength(2);

    await fireEvent.press(await screen.findByRole("button", { name: "İptal et" }));
    await fireEvent.press(screen.getByRole("button", { name: "Evet, iptal et" }));
    expect(await screen.findByText("Başlamış randevu iptal edilemez.")).toBeTruthy();
  });

  it("randevu al: tek çocuk → öğretmen → gün gün boş saatler; konu hatası istek göndermez; 'Bu saat artık dolu.' liste yeniler; başarı", async () => {
    let release: () => void = () => undefined;
    const taken = new Promise<Response>((resolve) => {
      release = () => resolve(business("Bu saat artık dolu."));
    });
    let posts = 0;
    let slotCalls = 0;
    const requests = fakeApi({
      mine: () => json(200, { upcoming: [], past: [] }),
      children: () => json(200, [ali]),
      teachers: () => json(200, [teacher]),
      slots: () => json(200, ++slotCalls === 1 ? [slot1, slot2] : [slot2]),
      request: () => (++posts === 1 ? taken : json(200, { appointmentId: "a9", state: "Pending" })),
    });
    await signIn();
    await openMine();
    await fireEvent.press(await screen.findByRole("button", { name: "Randevu al" }));
    await fireEvent.press(await screen.findByRole("button", { name: "Selin Öğretmen" }));
    expect(await screen.findByText("Perşembe 26 Kasım")).toBeTruthy();
    expect(screen.getByTestId("book-list").type).toBe("RCTScrollView");
    await fireEvent.press(screen.getByRole("button", { name: "Perşembe 26 Kasım 14:00, boş" }));
    expect(screen.getByText("Seçilen saat: Perşembe 26 Kasım 14:00")).toBeTruthy();

    await fireEvent.press(screen.getByRole("button", { name: "Talep gönder" }));
    expect(await screen.findByText("Konu zorunludur")).toBeTruthy();
    expect(requests.some((r) => r.method === "POST" && r.path === "/api/appointments")).toBe(false);

    await fireEvent.changeText(screen.getByLabelText("Konu"), "  Matematik notları  ");
    await fireEvent.press(screen.getByRole("button", { name: "Talep gönder" }));
    expect(await screen.findByText("Gönderiliyor…")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Gönderiliyor…" })).toBeDisabled();
    release();
    expect(await screen.findByText("Bu saat artık dolu.")).toBeTruthy();
    expect(await screen.findByRole("button", { name: "Perşembe 26 Kasım 14:20, boş" })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "Perşembe 26 Kasım 14:00, boş" })).toBeNull();
    expect(requests.find((r) => r.method === "POST" && r.path === "/api/appointments")!.body).toEqual({ slotId: "s1", studentId: "c1", topic: "Matematik notları" });

    await fireEvent.press(screen.getByRole("button", { name: "Perşembe 26 Kasım 14:20, boş" }));
    await fireEvent.press(screen.getByRole("button", { name: "Talep gönder" }));
    expect(await screen.findByText("Talebiniz öğretmene iletildi")).toBeTruthy();
    expect(screen.getByTestId("my-appointments")).toBeTruthy();
  });

  it("randevu al boş durumları: çocuk yok; birden çok çocukta seçim; öğretmen yok; boş saat yok", async () => {
    let kids: unknown[] = [];
    fakeApi({
      mine: () => json(200, { upcoming: [], past: [] }),
      children: () => json(200, kids),
      teachers: (request) => json(200, new URL(request.url).searchParams.get("studentId") === "c2" ? [] : [teacher]),
      slots: () => json(200, []),
    });
    await signIn();
    await openMine();
    await fireEvent.press(await screen.findByRole("button", { name: "Randevu al" }));
    expect(await screen.findByText("Kayıtlı çocuğunuz yok")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Geri" }));

    kids = [ali, can];
    await fireEvent.press(await screen.findByRole("button", { name: "Randevu al" }));
    expect(await screen.findByText("Çocuk seçin")).toBeTruthy();
    expect(screen.queryByText("Öğretmen seçin")).toBeNull();
    await fireEvent.press(screen.getByRole("button", { name: "Can Yılmaz" }));
    expect(await screen.findByText("Bu sınıfa atanmış öğretmen yok")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Ali Yılmaz" }));
    await fireEvent.press(await screen.findByRole("button", { name: "Selin Öğretmen" }));
    expect(await screen.findByText("Bu öğretmenin boş görüşme saati yok")).toBeTruthy();
  });

  it("veli olmayana 'Randevular' görünmez", async () => {
    fakeApi({ roles: ["Founder"] });
    await signIn();
    expect(screen.queryByRole("button", { name: "Randevular" })).toBeNull();
  });
});
