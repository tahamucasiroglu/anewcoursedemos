import { fireEvent, render, screen } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";
import { warmUpFirstRender } from "../../test/warmUp";

// Spec 0016 AC-13 (öğretmen ekranları, iki tema) ve AC-3: "Randevular (n bekleyen)" yalnızca öğretmende; Takvimim yükleniyor / boş
// ("Henüz müsaitlik açmadınız") / hata + tekrar dene / dolu — onay bekleyenler üstte, gün gün dilimler, dilim etiketi gün + saat + durum
// ("Perşembe 26 Kasım 14:00, boş"), liste kayan öğede (T16); boş dilimi kaldırma (işlenirken kilitli, sonuç duyurulur, hata metni);
// müsaitlik formu: alan hataları (istek gitmez), gönderiliyor-düğme kilitli, çakışma hatası (gerçek gövde), başarı ("6 görüşme saati
// açıldı"); talep yanıtı: onay (işleniyor-kilitli → sonuç), uzun ret nedeni alan hatası (istek gitmez), onaylı randevuda iptal onay
// iletişimi ve "Başlamış randevu iptal edilemez." hatası.
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const teacherMe = { fullName: "Ali Öğretmen", email: "ali@ornek.test", tenant: tenantA, roles: ["Teacher"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0, pendingAppointmentCount: 2 };

const approved = { slotId: "s1", startsAt: "2026-11-25T07:00:00Z", endsAt: "2026-11-25T07:30:00Z", state: "Approved", hasStarted: false, appointmentId: "a1", guardianName: "Ayla Kaya", studentName: "Ayşe Kaya", topic: "Devamsızlık" };
const free = { slotId: "s2", startsAt: "2026-11-26T11:00:00Z", endsAt: "2026-11-26T11:20:00Z", state: "Free", hasStarted: false, appointmentId: null, guardianName: null, studentName: null, topic: null };
const pending = { slotId: "s3", startsAt: "2026-11-26T11:20:00Z", endsAt: "2026-11-26T11:40:00Z", state: "Pending", hasStarted: false, appointmentId: "a2", guardianName: "Veli Yılmaz", studentName: "Ali Yılmaz", topic: "Matematik notları" };
const filled = { pendingCount: 1, pending: [pending], slots: [approved, free, pending] };
const empty = { pendingCount: 0, pending: [], slots: [] };

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

/** Sunucunun iş hatası gövdesi (BusinessException → 400, Türkçe `detail`). */
function business(message: string): Response {
  return new Response(JSON.stringify({ type: "https://example.com/probs/business", title: "Rule violation", status: 400, detail: message }), { status: 400, headers: { "Content-Type": "application/json" } });
}

/**
 * Sunucunun gerçek iş hatası gövdesi (T20 biçimi): Türkçe `detail` + büyük harfli `Errors` dizisi — `TraceIdExceptionMiddleware`
 * ilk alan mesajını `detail`'e kopyalar, dizi aynen kalır.
 */
function realBusiness(message: string): Response {
  return new Response(
    JSON.stringify({ type: "https://example.com/probs/business", title: "Rule violation", status: 400, detail: message, Errors: [{ Property: "Appointment", Errors: [message] }] }),
    { status: 400, headers: { "Content-Type": "application/problem+json" } },
  );
}

interface Handlers {
  roles?: string[];
  schedule?: Handler;
  open?: Handler;
  remove?: Handler;
  respond?: Handler;
  cancel?: Handler;
}

function fakeApi(handlers: Handlers) {
  const requests: { method: string; path: string; body: unknown }[] = [];
  globalThis.fetch = (async (request: Request) => {
    const path = new URL(request.url).pathname;
    const text = request.method === "POST" ? await request.clone().text() : "";
    requests.push({ method: request.method, path, body: text ? JSON.parse(text) : null });
    const route: [RegExp, string, Handler | undefined][] = [
      [/^\/api\/appointments\/schedule$/, "GET", handlers.schedule],
      [/^\/api\/appointments\/availability$/, "POST", handlers.open],
      [/^\/api\/appointments\/slots\/[^/]+$/, "DELETE", handlers.remove],
      [/^\/api\/appointments\/[^/]+\/respond$/, "POST", handlers.respond],
      [/^\/api\/appointments\/[^/]+\/cancel$/, "POST", handlers.cancel],
    ];
    if (path === "/api/system-health") {
      return json(200, { status: "healthy", message: "Sistem çalışıyor" });
    }

    if (path === "/api/auth/login") {
      return json(200, tokens);
    }

    if (path === "/api/me") {
      return json(200, { ...teacherMe, roles: handlers.roles ?? teacherMe.roles });
    }

    const match = route.find(([pattern, method, handler]) => pattern.test(path) && method === request.method && handler);
    return match ? match[2]!(request) : json(404, {});
  }) as typeof fetch;
  return requests;
}

async function signIn() {
  await render(<App />);
  await fireEvent.changeText(await screen.findByLabelText("E-posta"), "ali@ornek.test");
  await fireEvent.changeText(screen.getByLabelText("Şifre"), "Sifre-1234");
  await fireEvent.press(screen.getByText("Giriş yap"));
  await screen.findByTestId("welcome");
}

async function openSchedule() {
  await fireEvent.press(screen.getByRole("button", { name: "Randevular, 2 bekleyen talep" }));
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
  fakeApi({ schedule: () => json(200, filled) });
  await signIn();
  await openSchedule();
  await screen.findAllByTestId("schedule-slot");
  await fireEvent.press(screen.getByRole("button", { name: "Müsaitlik aç" }));
  await screen.findByTestId("open-availability");
});

describe.each(["light", "dark"] as const)("Randevular (öğretmen) — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("menü bekleyen sayısıyla; Takvimim yükleniyor → boş → (hata + tekrar dene) → dolu: onay bekleyen üstte, etiketler gün + saat + durum", async () => {
    let release: () => void = () => undefined;
    const first = new Promise<Response>((resolve) => {
      release = () => resolve(json(200, empty));
    });
    let calls = 0;
    fakeApi({ schedule: () => (++calls === 1 ? first : calls === 2 ? json(500, { detail: "Beklenmeyen hata" }) : json(200, filled)) });
    await signIn();
    expect(screen.getByText("Randevular (2 bekleyen)")).toBeTruthy();
    await openSchedule();
    expect(await screen.findByText("Yükleniyor…")).toBeTruthy();
    release();
    expect(await screen.findByText("Henüz müsaitlik açmadınız")).toBeTruthy();

    await fireEvent.press(screen.getByRole("button", { name: "Geri" }));
    await openSchedule();
    expect(await screen.findByText("Beklenmeyen hata")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));

    expect(await screen.findByText("Onay bekleyen talepler")).toBeTruthy();
    expect(screen.getByTestId("schedule-list").type).toBe("RCTScrollView");
    expect(screen.getAllByTestId("pending-request")).toHaveLength(1);
    expect(screen.getByText("Matematik notları")).toBeTruthy();
    expect(screen.getByText("Çarşamba 25 Kasım")).toBeTruthy();
    expect(screen.getByText("Perşembe 26 Kasım")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Perşembe 26 Kasım 14:00, boş" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Çarşamba 25 Kasım 10:00, onaylı" })).toBeTruthy();
    expect(screen.getAllByText("14:20 · onay bekliyor")).toHaveLength(1);
    expect(screen.getAllByRole("button", { name: /^Kaldır:/ })).toHaveLength(1);
  });

  it("boş dilimi kaldırma: işlenirken kilitli → 'Görüşme saati kaldırıldı.'; sunucu hatası duyurulur", async () => {
    let release: () => void = () => undefined;
    const removal = new Promise<Response>((resolve) => {
      release = () => resolve(new Response(null, { status: 204 }));
    });
    let removals = 0;
    const requests = fakeApi({ schedule: () => json(200, filled), remove: () => (++removals === 1 ? removal : business("Önce talebi yanıtlayın ya da randevuyu iptal edin.")) });
    await signIn();
    await openSchedule();
    await fireEvent.press(await screen.findByRole("button", { name: "Kaldır: Perşembe 26 Kasım 14:00, boş" }));
    expect(await screen.findByText("Kaldırılıyor…")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Kaldır: Perşembe 26 Kasım 14:00, boş" })).toBeDisabled();
    release();
    expect(await screen.findByText("Görüşme saati kaldırıldı.")).toBeTruthy();
    expect(requests.filter((r) => r.method === "DELETE").map((r) => r.path)).toEqual(["/api/appointments/slots/s2"]);

    await fireEvent.press(await screen.findByRole("button", { name: "Kaldır: Perşembe 26 Kasım 14:00, boş" }));
    expect(await screen.findByText("Önce talebi yanıtlayın ya da randevuyu iptal edin.")).toBeTruthy();
    expect(requests.filter((r) => r.path === "/api/appointments/schedule").length).toBeGreaterThanOrEqual(3);
  });

  it("müsaitlik formu: alan hataları istek göndermez → gönderiliyor kilitli → çakışma hatası → başarı '6 görüşme saati açıldı'", async () => {
    let release: () => void = () => undefined;
    const overlap = new Promise<Response>((resolve) => {
      release = () => resolve(business("Bu saatlerde zaten müsaitliğiniz var."));
    });
    let posts = 0;
    const requests = fakeApi({ schedule: () => json(200, empty), open: () => (++posts === 1 ? overlap : json(200, { slotCount: 6 })) });
    await signIn();
    await openSchedule();
    await fireEvent.press(await screen.findByRole("button", { name: "Müsaitlik aç" }));
    expect(await screen.findByText("6 görüşme saati açılacak")).toBeTruthy();

    await fireEvent.changeText(screen.getByLabelText("Başlangıç (SS:DD)"), "14:10");
    await fireEvent.changeText(screen.getByLabelText("Bitiş (SS:DD)"), "14:50");
    await fireEvent.changeText(screen.getByLabelText("Gün (GG.AA.YYYY)"), "31.02.2026");
    await fireEvent.press(screen.getByRole("button", { name: "30 dakika" }));
    await fireEvent.press(screen.getByRole("button", { name: "Aç" }));
    expect(await screen.findByText("Başlangıç saati SS:DD biçiminde ve 15 dakikalık adımlarla olmalı")).toBeTruthy();
    expect(screen.getByText("Bitiş saati SS:DD biçiminde ve 15 dakikalık adımlarla olmalı")).toBeTruthy();
    expect(screen.getByText("Gün GG.AA.YYYY biçiminde olmalı")).toBeTruthy();
    expect(requests.some((r) => r.path === "/api/appointments/availability")).toBe(false);

    await fireEvent.press(screen.getByRole("button", { name: "Yarın" }));
    await fireEvent.changeText(screen.getByLabelText("Başlangıç (SS:DD)"), "14:00");
    await fireEvent.changeText(screen.getByLabelText("Bitiş (SS:DD)"), "17:00");
    await fireEvent.press(screen.getByRole("button", { name: "Aç" }));
    expect(await screen.findByText("Açılıyor…")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Açılıyor…" })).toBeDisabled();
    release();
    expect(await screen.findByText("Bu saatlerde zaten müsaitliğiniz var.")).toBeTruthy();
    const body = requests.find((r) => r.path === "/api/appointments/availability")!.body as { day: string; start: string; end: string; durationMinutes: number };
    expect(body).toMatchObject({ start: "14:00", end: "17:00", durationMinutes: 30 });
    expect(body.day).toMatch(/^\d{4}-\d{2}-\d{2}$/);

    await fireEvent.press(screen.getByRole("button", { name: "Aç" }));
    expect(await screen.findByText("6 görüşme saati açıldı")).toBeTruthy();
    expect(screen.getByTestId("teacher-schedule")).toBeTruthy();
  });

  it("talep yanıtı: onay işlenirken kilitli → 'Talep onaylandı.'; uzun ret nedeni istek göndermez", async () => {
    let release: () => void = () => undefined;
    const approval = new Promise<Response>((resolve) => {
      release = () => resolve(json(200, { appointmentId: "a2", state: "Approved" }));
    });
    const requests = fakeApi({ schedule: () => json(200, filled), respond: () => approval });
    await signIn();
    await openSchedule();
    await fireEvent.press((await screen.findAllByTestId("pending-request"))[0]);
    expect(await screen.findByText("Veli: Veli Yılmaz")).toBeTruthy();
    expect(screen.getByText("Öğrenci: Ali Yılmaz")).toBeTruthy();
    expect(screen.getByText("Durum: onay bekliyor")).toBeTruthy();

    await fireEvent.changeText(screen.getByLabelText("Neden (isteğe bağlı)"), "n".repeat(301));
    await fireEvent.press(screen.getByRole("button", { name: "Reddet" }));
    expect(await screen.findByText("Neden en fazla 300 karakter olabilir")).toBeTruthy();
    expect(requests.some((r) => r.path.endsWith("/respond"))).toBe(false);

    await fireEvent.press(screen.getByRole("button", { name: "Onayla" }));
    expect(await screen.findByText("İşleniyor…")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Reddet" })).toBeDisabled();
    release();
    expect(await screen.findByText("Talep onaylandı.")).toBeTruthy();
    expect(requests.find((r) => r.path.endsWith("/respond"))).toMatchObject({ path: "/api/appointments/a2/respond", body: { decision: "Approve" } });
  });

  it("onaylı randevuda iptal onay iletişimi; 'Başlamış randevu iptal edilemez.' hatası duyurulur; Vazgeç istek göndermez", async () => {
    const requests = fakeApi({ schedule: () => json(200, filled), cancel: () => business("Başlamış randevu iptal edilemez.") });
    await signIn();
    await openSchedule();
    await fireEvent.press(await screen.findByRole("button", { name: "Çarşamba 25 Kasım 10:00, onaylı" }));
    await fireEvent.press(await screen.findByRole("button", { name: "İptal et" }));
    expect(screen.getByText("Randevu iptal edilsin mi?")).toBeTruthy();
    await fireEvent.press(screen.getByRole("button", { name: "Vazgeç" }));
    expect(requests.some((r) => r.path.endsWith("/cancel"))).toBe(false);

    await fireEvent.press(screen.getByRole("button", { name: "İptal et" }));
    await fireEvent.press(screen.getByRole("button", { name: "Evet, iptal et" }));
    expect(await screen.findByText("Başlamış randevu iptal edilemez.")).toBeTruthy();
    expect(requests.find((r) => r.path.endsWith("/cancel"))).toMatchObject({ path: "/api/appointments/a1/cancel", body: { reason: null } });
  });

  it.each([
    {
      when: "veli başka cihazda iptal etti: dilim boşaldı → 'Bu talep artık geçerli değil.', düğmeler kalkar",
      after: { pendingCount: 0, pending: [], slots: [approved, free, { ...free, slotId: "s3", startsAt: pending.startsAt, endsAt: pending.endsAt }] },
      shows: "Bu talep artık geçerli değil.",
      cancel: false,
    },
    {
      when: "başka cihazda onaylandı → 'Durum: onaylı', Onayla/Reddet kalkar, İptal et görünür",
      after: { pendingCount: 0, pending: [], slots: [approved, free, { ...pending, state: "Approved" }] },
      shows: "Durum: onaylı",
      cancel: true,
    },
  ])("talep yanıtı hatasında ('Bu talep zaten yanıtlandı.') talep yeniden okunur — $when (VERIFY V-1)", async ({ after, shows, cancel }) => {
    let calls = 0;
    const requests = fakeApi({ schedule: () => json(200, ++calls === 1 ? filled : after), respond: () => realBusiness("Bu talep zaten yanıtlandı.") });
    await signIn();
    await openSchedule();
    await fireEvent.press((await screen.findAllByTestId("pending-request"))[0]);
    expect(await screen.findByText("Durum: onay bekliyor")).toBeTruthy();

    await fireEvent.press(screen.getByRole("button", { name: "Onayla" }));
    expect(await screen.findByText(shows)).toBeTruthy();
    expect(screen.getByText("Bu talep zaten yanıtlandı.")).toBeTruthy();
    expect(screen.queryByText("Durum: onay bekliyor")).toBeNull();
    expect(screen.queryByRole("button", { name: "Onayla" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Reddet" })).toBeNull();
    expect(screen.queryByRole("button", { name: "İptal et" }) !== null).toBe(cancel);
    expect(screen.getByTestId("appointment-detail")).toBeTruthy();
    expect(requests.filter((r) => r.path === "/api/appointments/schedule")).toHaveLength(2);
  });

  it("öğretmen olmayana 'Randevular (n bekleyen)' görünmez", async () => {
    fakeApi({ roles: ["Founder"] });
    await signIn();
    expect(screen.queryByText(/^Randevular/)).toBeNull();
  });
});
