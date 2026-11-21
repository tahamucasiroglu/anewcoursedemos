import { fireEvent, render, screen } from "@testing-library/react-native";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { THEME_STORAGE_KEY, themeColors } from "@okul/theme";
import { StyleSheet } from "react-native";
import * as SecureStore from "expo-secure-store";
import App from "../../../App";
import { warmUpFirstRender } from "../../test/warmUp";

// Spec 0015 AC-11 (veli görünümü) ve AC-13 (mobil, iki tema): "Ödemeler" yalnızca velide; toplam kalan, çocuk kartları (farklı şubeler),
// planı olmayan çocukta / hiç planı olmayan velide "Henüz ödeme planı yok"; plan ayrıntısında taksitler ("Gecikti" metinle) ve
// tahsilatlar (makbuz no, "İPTAL EDİLDİ"); ödeme ya da düzenleme düğmesi yok; yükleniyor, hata + tekrar dene, "artık
// görüntülenemiyor" (404); erişilebilirlik etiketleri; listeler kayan öğede (T16).
type Handler = (request: Request) => Promise<Response> | Response;

const originalFetch = globalThis.fetch;
const tenantA = { id: "11111111-1111-4111-8111-111111111111", name: "A Koleji" };
const reset = (SecureStore as unknown as { __reset: () => void }).__reset;
const tokens = { accessToken: "erisim", accessTokenExpiresAt: "2026-11-20T10:15:00Z", refreshToken: "yenileme-1", requiresTenantSelection: false, tenants: [tenantA] };
const guardianMe = { fullName: "Ayla Ak", email: "ayla@ornek.test", tenant: tenantA, roles: ["Guardian"], branches: [{ id: "b1", name: "Merkez" }], tenants: [tenantA], isPlatformAdmin: false, unreadNoticeCount: 0 };

const zeynepPlan = {
  id: "p1",
  feeName: "2026-2027 Eğitim Ücreti",
  totals: { net: 8500, paid: 1500, remaining: 7000, overdue: 1333.33 },
  installments: [
    { sequence: 1, dueDate: "2026-11-01", amount: 2833.33, paid: 1500, remaining: 1333.33, state: "Overdue" },
    { sequence: 2, dueDate: "2026-12-01", amount: 2833.33, paid: 0, remaining: 2833.33, state: "Pending" },
    { sequence: 3, dueDate: "2027-01-01", amount: 2833.34, paid: 0, remaining: 2833.34, state: "Pending" },
  ],
  payments: [
    { date: "2026-11-05", amount: 1500, method: "Cash", receiptNo: 7, cancelled: false },
    { date: "2026-11-06", amount: 250, method: "BankTransfer", receiptNo: 8, cancelled: true },
  ],
};
const zeynep = { studentId: "c1", fullName: "Zeynep Ak", branchName: "Merkez", classroomName: "3-A", totals: zeynepPlan.totals, plans: [zeynepPlan] };
const can = { studentId: "c2", fullName: "Can Ak", branchName: "Avcılar", classroomName: null, totals: { net: 1000, paid: 0, remaining: 1000, overdue: 0 }, plans: [{ ...zeynepPlan, id: "p2", feeName: "Kurs", totals: { net: 1000, paid: 0, remaining: 1000, overdue: 0 }, installments: [{ sequence: 1, dueDate: "2026-12-15", amount: 1000, paid: 0, remaining: 1000, state: "Pending" }], payments: [] }] };
const deniz = { studentId: "c3", fullName: "Deniz Ak", branchName: "Merkez", classroomName: null, totals: { net: 0, paid: 0, remaining: 0, overdue: 0 }, plans: [] };

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": status >= 400 ? "application/problem+json" : "application/json" } });
}

function fakeApi(handlers: { list?: Handler; child?: Handler; roles?: string[] }) {
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
      return json(200, { ...guardianMe, roles: handlers.roles ?? guardianMe.roles });
    }

    if (path === "/api/my/payments" && handlers.list) {
      return handlers.list(request);
    }

    if (/^\/api\/my\/payments\/[^/]+$/.test(path) && handlers.child) {
      return handlers.child(request);
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

// İlk çizimin soğuk başlangıç bedeli testlerden önce, doğrulamasız bir turda ödenir (conventions T21; mobile/src/test/warmUp.ts).
warmUpFirstRender(async () => {
  fakeApi({ list: () => json(200, { totalRemaining: 8000, children: [zeynep, can] }), child: () => json(200, zeynep) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Ödemeler" }));
  await fireEvent.press((await screen.findAllByTestId("payments-child"))[0]);
  await screen.findAllByTestId("child-installment");
});

describe.each(["light", "dark"] as const)("Ödemeler (veli) — %s tema", (mode) => {
  beforeEach(async () => {
    await AsyncStorage.setItem(THEME_STORAGE_KEY, mode);
  });

  afterEach(() => {
    expect(StyleSheet.flatten(screen.getByTestId("screen").props.style).backgroundColor).toBe(themeColors[mode].background);
  });

  it("yükleniyor → toplam kalan ve çocuk kartları (farklı şubeler) → plan ayrıntısı: taksitler 'Gecikti' metinle, tahsilatlar, iptal", async () => {
    let release: () => void = () => undefined;
    const pending = new Promise<Response>((resolve) => {
      release = () => resolve(json(200, { totalRemaining: 8000, children: [zeynep, can, deniz] }));
    });
    const requests = fakeApi({ list: () => pending, child: () => json(200, zeynep) });
    await signIn();
    await fireEvent.press(screen.getByRole("button", { name: "Ödemeler" }));
    expect(await screen.findByText("Yükleniyor…")).toBeTruthy();
    release();

    expect(await screen.findByText("Toplam kalan: 8.000,00 TL")).toBeTruthy();
    expect(screen.getByTestId("payments-list").type).toBe("RCTScrollView");
    expect(screen.getAllByTestId("payments-child").map((c) => c.props.accessibilityLabel)).toEqual([
      "Zeynep Ak, Merkez, kalan 7.000,00 TL, aç",
      "Can Ak, Avcılar, kalan 1.000,00 TL, aç",
      "Deniz Ak, Merkez, kalan 0,00 TL, aç",
    ]);
    expect(screen.getByText("Geciken: 1.333,33 TL")).toBeTruthy();
    expect(screen.getByText("Henüz ödeme planı yok")).toBeTruthy();
    expect(screen.queryByRole("button", { name: /Öde|Ödeme yap|Düzenle/ })).toBeNull();

    await fireEvent.press(screen.getByRole("button", { name: "Zeynep Ak, Merkez, kalan 7.000,00 TL, aç" }));
    expect(await screen.findByRole("header", { name: "Zeynep Ak" })).toBeTruthy();
    expect(screen.getByTestId("child-plan-scroll").type).toBe("RCTScrollView");
    expect(screen.getAllByTestId("child-installment").map((r) => r.props.accessibilityLabel)).toEqual([
      "1. taksit · vade 01.11.2026 · 2.833,33 TL, Gecikti",
      "2. taksit · vade 01.12.2026 · 2.833,33 TL, Bekliyor",
      "3. taksit · vade 01.01.2027 · 2.833,34 TL, Bekliyor",
    ]);
    expect(screen.getByText("Gecikti")).toBeTruthy();
    expect(screen.getAllByTestId("child-payment").map((r) => r.props.accessibilityLabel)).toEqual([
      "05.11.2026 · 1.500,00 TL · Nakit · makbuz 7",
      "06.11.2026 · 250,00 TL · Havale/EFT · makbuz 8, İPTAL EDİLDİ",
    ]);
    expect(screen.getByText("Net 8.500,00 TL · ödenen 1.500,00 TL · kalan 7.000,00 TL")).toBeTruthy();
    expect(requests).toContain("GET /api/my/payments/c1");
    expect(requests.some((r) => r.startsWith("POST /api/billing") || r.startsWith("POST /api/my/payments"))).toBe(false);

    await fireEvent.press(screen.getByRole("button", { name: "Ödemelere dön" }));
    expect(await screen.findByText("Toplam kalan: 8.000,00 TL")).toBeTruthy();
  });

  it("hiç planı olmayan veli: 'Henüz ödeme planı yok'", async () => {
    fakeApi({ list: () => json(200, { totalRemaining: 0, children: [deniz] }) });
    await signIn();
    await fireEvent.press(screen.getByRole("button", { name: "Ödemeler" }));
    expect(await screen.findByText("Henüz ödeme planı yok")).toBeTruthy();
    expect(screen.queryByTestId("payments-child")).toBeNull();
    await fireEvent.press(screen.getByRole("button", { name: "Geri" }));
    expect(await screen.findByTestId("welcome")).toBeTruthy();
  });
});

it("liste hatası + tekrar dene; ayrıntıda 404 → 'Bu öğrenci artık görüntülenemiyor.'", async () => {
  let calls = 0;
  fakeApi({ list: () => (++calls === 1 ? json(500, {}) : json(200, { totalRemaining: 7000, children: [zeynep] })), child: () => json(404, { detail: "Öğrenci bulunamadı" }) });
  await signIn();
  await fireEvent.press(screen.getByRole("button", { name: "Ödemeler" }));
  expect(await screen.findByText("Ödeme bilgileri alınamadı.")).toBeTruthy();
  await fireEvent.press(screen.getByRole("button", { name: "Tekrar dene" }));
  await fireEvent.press((await screen.findAllByTestId("payments-child"))[0]);
  expect(await screen.findByRole("alert")).toHaveTextContent("Bu öğrenci artık görüntülenemiyor.");
});

it("öğretmen rolünde 'Ödemeler' yok ve istek gitmez", async () => {
  const requests = fakeApi({ roles: ["Teacher"] });
  await signIn();
  expect(screen.queryByRole("button", { name: "Ödemeler" })).toBeNull();
  expect(requests.some((r) => r.includes("/api/my/payments"))).toBe(false);
});
